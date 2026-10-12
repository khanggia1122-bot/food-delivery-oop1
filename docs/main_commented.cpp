// ===== KHAI BAO THU VIEN =====
#include <iostream>   // cout, endl de in ra man hinh
#include <string>     // kieu string, to_string
#include <cmath>      // sqrt de tinh khoang cach
#include <stdexcept>  // runtime_error de bao loi khi thao tac khong hop le

using namespace std;

// ===== HANG SO =====
// Dung mang tho nen phai co kich thuoc toi da co dinh
const int MAX_MENU = 10;     // so mon toi da trong menu cua 1 nha hang
const int MAX_ITEMS = 20;    // so mon toi da trong 1 don hang
const int MAX_DRIVERS = 10;  // so tai xe toi da trong mang tai xe

// ===== VI TRI + KHOANG CACH =====
struct Location {
    double x;  // toa do x (don vi km)
    double y;  // toa do y (don vi km)
};

// Khoang cach Euclid giua 2 diem: sqrt(dx^2 + dy^2)
double distanceKm(const Location& a, const Location& b) {
    double dx = a.x - b.x;
    double dy = a.y - b.y;
    return sqrt(dx * dx + dy * dy);
}

// ===== TRANG THAI DON HANG =====
// Enum gan so 0..5 theo thu tu, cac so nay dung lam chi so cho bang chuyen trang thai
enum OrderState {
    PLACED,            // 0: vua dat
    CONFIRMED,         // 1: nha hang da xac nhan
    PREPARING,         // 2: dang nau
    OUT_FOR_DELIVERY,  // 3: tai xe dang giao
    DELIVERED,         // 4: da giao (ket thuc)
    CANCELLED          // 5: da huy (ket thuc)
};

// Doi trang thai thanh chuoi de in
string stateName(OrderState s) {
    switch (s) {
        case PLACED: return "Placed";
        case CONFIRMED: return "Confirmed";
        case PREPARING: return "Preparing";
        case OUT_FOR_DELIVERY: return "OutForDelivery";
        case DELIVERED: return "Delivered";
        case CANCELLED: return "Cancelled";
    }
    return "Unknown";  // khong bao gio toi day, chi de compiler khong canh bao
}

// ===== MON AN + DECORATOR =====
// Lop truu tuong: dinh nghia "mot mon an" phai biet noi ten va tra gia
class MenuItem {
public:
    virtual ~MenuItem() {}                      // destructor ao: xoa qua con tro co so van goi dung destructor lop con
    virtual string getName() const = 0;         // = 0: thuan ao, lop con bat buoc cai dat
    virtual long long getPrice() const = 0;     // gia tinh bang VND
};

// Mon co ban (long nhat cua chuoi decorator): chi co ten va gia goc
class BasicItem : public MenuItem {
private:
    string name;
    long long price;
public:
    BasicItem(const string& n, long long p) : name(n), price(p) {}
    string getName() const { return name; }
    long long getPrice() const { return price; }
};

// Lop decorator goc: "la mot MenuItem" (ke thua) va "co mot MenuItem" (con tro inner)
// Day la chinh la composition: tuy bien bang cach boc, khong them lop con cho moi mon
class ItemDecorator : public MenuItem {
protected:
    MenuItem* inner;   // mon ben trong dang duoc boc
public:
    ItemDecorator(MenuItem* i) : inner(i) {}
    virtual ~ItemDecorator() { delete inner; }  // huy lop ngoai thi huy luon ca chuoi ben trong
};

// Them topping: gia = gia ben trong + gia topping; ten noi them " + ten topping"
class Topping : public ItemDecorator {
private:
    string toppingName;
    long long extraPrice;
public:
    Topping(MenuItem* i, const string& n, long long p)
        : ItemDecorator(i), toppingName(n), extraPrice(p) {}
    string getName() const { return inner->getName() + " + " + toppingName; }
    long long getPrice() const { return inner->getPrice() + extraPrice; }
};

// Nang size: nhan gia ben trong voi phan tram (130 nghia la tang 30%)
class SizeUpgrade : public ItemDecorator {
private:
    string sizeName;
    int percent;
public:
    SizeUpgrade(MenuItem* i, const string& n, int pct)
        : ItemDecorator(i), sizeName(n), percent(pct) {}
    string getName() const { return inner->getName() + " (" + sizeName + ")"; }
    long long getPrice() const { return inner->getPrice() * percent / 100; }
};

// ===== NHA HANG =====
class Restaurant {
private:
    string name;
    Location location;
    MenuItem* menu[MAX_MENU];  // mang tho cac con tro mon (Restaurant so huu)
    int menuCount;             // so mon hien co
public:
    Restaurant(const string& n, Location loc) : name(n), location(loc), menuCount(0) {}
    // Cam sao chep: neu sao chep se co 2 doi tuong cung delete mot con tro (loi double delete)
    Restaurant(const Restaurant&) = delete;
    Restaurant& operator=(const Restaurant&) = delete;
    ~Restaurant() {
        for (int i = 0; i < menuCount; i++) delete menu[i];  // giai phong tung mon
    }
    void addMenuItem(const string& itemName, long long price) {
        if (menuCount >= MAX_MENU) throw runtime_error("Menu is full");
        menu[menuCount++] = new BasicItem(itemName, price);  // tao mon moi roi tang dem
    }
    // Tra ve BAN SAO moi cua mon trong menu de don hang tu do tuy bien/xoa ma khong dong den menu goc
    MenuItem* createItem(int index) const {
        if (index < 0 || index >= menuCount) throw runtime_error("Menu item does not exist");
        return new BasicItem(menu[index]->getName(), menu[index]->getPrice());
    }
    const string& getName() const { return name; }
    const Location& getLocation() const { return location; }
    int getMenuCount() const { return menuCount; }
    void printMenu() const {
        cout << "Menu of " << name << ":" << endl;
        for (int i = 0; i < menuCount; i++) {
            cout << "  [" << i << "] " << menu[i]->getName() << " - " << menu[i]->getPrice() << " VND" << endl;
        }
    }
};

// ===== CHINH SACH PHI GIAO HANG (Strategy) =====
class FeePolicy {
public:
    virtual ~FeePolicy() {}
    virtual long long fee(double km) const = 0;  // phi theo so km
    virtual string name() const = 0;             // ten chinh sach de in
};

// Phi co dinh + phi theo km
class DistanceFee : public FeePolicy {
private:
    long long baseFee;
    long long perKm;
public:
    DistanceFee(long long b, long long p) : baseFee(b), perKm(p) {}
    // +0.5 de lam tron: ep kieu (long long) se cat phan thap phan nen cong 0.5 truoc de ra lam tron
    long long fee(double km) const { return baseFee + (long long)(perKm * km + 0.5); }
    string name() const { return "DistanceFee"; }
};

// Surge: boc mot chinh sach khac va nhan he so (150 = tang 50%)
class SurgeFee : public FeePolicy {
private:
    const FeePolicy* inner;  // khong so huu, chi tro toi policy co san
    int percent;
public:
    SurgeFee(const FeePolicy* i, int pct) : inner(i), percent(pct) {}
    long long fee(double km) const { return inner->fee(km) * percent / 100; }
    string name() const { return "SurgeFee(" + inner->name() + " x" + to_string(percent) + "%)"; }
};

// ===== TAI XE =====
class DeliveryDriver {
private:
    string name;
    Location location;
    bool available;  // true = dang ranh
public:
    DeliveryDriver(const string& n, Location loc) : name(n), location(loc), available(true) {}
    const string& getName() const { return name; }
    const Location& getLocation() const { return location; }
    bool isAvailable() const { return available; }
    // Nhan don: neu dang ban thi bao loi (chong double-booking)
    void reserve() {
        if (!available) throw runtime_error("Driver " + name + " is already busy");
        available = false;
    }
    // Giao xong: tro lai ranh va dung tai noi giao (cap nhat vi tri)
    void release(const Location& newLocation) {
        available = true;
        location = newLocation;
    }
};

// ===== DON HANG =====
class Order {
private:
    int id;
    const Restaurant* restaurant;
    Location destination;           // dia chi khach
    const FeePolicy* policy;        // chinh sach phi hien tai (khong so huu)
    MenuItem* items[MAX_ITEMS];     // cac mon da tuy bien (Order so huu, se delete)
    int itemCount;
    OrderState state;               // PRIVATE: chi Order moi duoc doi trang thai
    DeliveryDriver* driver;         // 0 (null) neu chua gan

    // Bang chuyen trang thai: table[tu][den] = true neu duoc phep
    // Hang = trang thai hien tai, cot = trang thai muon toi, thu tu theo enum
    //                    PLA    CON    PRE    OUT    DEL    CAN
    static bool canMove(OrderState from, OrderState to) {
        static const bool table[6][6] = {
            {false, true,  false, false, false, true },  // Placed    -> Confirmed, Cancelled
            {false, false, true,  false, false, true },  // Confirmed -> Preparing, Cancelled
            {false, false, false, true,  false, false},  // Preparing -> OutForDelivery (khong huy duoc)
            {false, false, false, false, true,  false},  // OutForDelivery -> Delivered
            {false, false, false, false, false, false},  // Delivered: ket thuc
            {false, false, false, false, false, false}   // Cancelled: ket thuc
        };
        return table[from][to];  // enum tu chuyen thanh so nguyen lam chi so
    }

    // Moi thay doi trang thai deu phai qua day
    void moveTo(OrderState next) {
        if (!canMove(state, next)) {
            throw runtime_error("Invalid transition: " + stateName(state) + " -> " + stateName(next));
        }
        state = next;
    }

public:
    Order(int orderId, const Restaurant* r, Location dest, const FeePolicy* p)
        : id(orderId), restaurant(r), destination(dest), policy(p),
          itemCount(0), state(PLACED), driver(0) {}  // don moi luon bat dau o Placed
    Order(const Order&) = delete;
    Order& operator=(const Order&) = delete;
    ~Order() {
        for (int i = 0; i < itemCount; i++) delete items[i];
    }

    // Order nhan quyen so huu mon ngay khi duoc goi, ke ca khi bi tu choi (nen phai delete truoc khi throw, neu khong se ro bo nho)
    void addItem(MenuItem* item) {
        if (state != PLACED) {  // chi cho them mon khi chua xac nhan
            delete item;
            throw runtime_error("Cannot add items when order is " + stateName(state));
        }
        if (itemCount >= MAX_ITEMS) {
            delete item;
            throw runtime_error("Order is full");
        }
        items[itemCount++] = item;
    }

    void setFeePolicy(const FeePolicy* p) { policy = p; }  // doi chinh sach luc chay

    long long subtotal() const {  // tong tien mon
        long long sum = 0;
        for (int i = 0; i < itemCount; i++) sum += items[i]->getPrice();  // goi ao, moi mon tu tinh gia (ke ca decorator)
        return sum;
    }
    double distance() const { return distanceKm(restaurant->getLocation(), destination); }
    long long deliveryFee() const { return policy->fee(distance()); }  // goi ao, khong biet policy cu the
    long long total() const { return subtotal() + deliveryFee(); }     // yeu cau 4

    // Cac thao tac chi la goi moveTo: luat nam het trong bang
    void confirm() { moveTo(CONFIRMED); }
    void startPreparing() { moveTo(PREPARING); }
    void cancel() { moveTo(CANCELLED); }  // khong co if rieng: bang quyet dinh duoc huy hay khong

    // Gan tai xe + chuyen sang OutForDelivery
    void dispatch(DeliveryDriver* d) {
        if (d == 0) throw runtime_error("No driver given");
        if (state != PREPARING) {  // kiem tra trang thai truoc de bao dung loi
            throw runtime_error("Invalid transition: " + stateName(state) + " -> " + stateName(OUT_FOR_DELIVERY));
        }
        if (!d->isAvailable()) throw runtime_error("Driver " + d->getName() + " is already busy");
        moveTo(OUT_FOR_DELIVERY);  // chuyen trang thai truoc
        d->reserve();              // roi moi danh dau tai xe ban (khong the loi vi vua kiem tra)
        driver = d;
    }

    void deliver() {
        moveTo(DELIVERED);              // sai trang thai thi throw ngay, chua dong den tai xe
        driver->release(destination);   // tra tai xe ve trang thai ranh tai dia chi khach
    }

    int getId() const { return id; }
    OrderState getState() const { return state; }
    DeliveryDriver* getDriver() const { return driver; }
    const Restaurant* getRestaurant() const { return restaurant; }

    void print() const {
        cout << "Order #" << id << " from " << restaurant->getName()
             << " | state: " << stateName(state) << endl;
        for (int i = 0; i < itemCount; i++) {
            cout << "  - " << items[i]->getName() << ": " << items[i]->getPrice() << " VND" << endl;
        }
        cout << "  Subtotal: " << subtotal() << " VND" << endl;
        cout << "  Distance: " << distance() << " km, fee (" << policy->name() << "): "
             << deliveryFee() << " VND" << endl;
        cout << "  TOTAL: " << total() << " VND" << endl;
        if (driver != 0) cout << "  Driver: " << driver->getName() << endl;
    }
};

// ===== GAN TAI XE (tach rieng, Single Responsibility) =====
class DriverAssigner {
public:
    // Chi lam 1 viec: chon tai xe ranh gan quan nhat roi nho Order tu dispatch
    DeliveryDriver* assign(Order& order, DeliveryDriver** drivers, int n) const {
        const Location& from = order.getRestaurant()->getLocation();  // tai xe phai toi quan lay mon
        DeliveryDriver* best = 0;
        double bestDist = 0;
        for (int i = 0; i < n; i++) {
            if (!drivers[i]->isAvailable()) continue;  // bo qua tai xe ban
            double d = distanceKm(from, drivers[i]->getLocation());
            if (best == 0 || d < bestDist) {  // tai xe dau tien hoac gan hon
                best = drivers[i];
                bestDist = d;
            }
        }
        if (best == 0) throw runtime_error("No available driver");
        order.dispatch(best);  // Order tu quyet dinh co chap nhan khong (vd sai trang thai thi throw)
        return best;
    }
};

// ===== HAM HO TRO IN KET QUA TEST =====
void printResult(const string& title, bool rejected, const string& msg) {
    cout << "  " << title << ": " << (rejected ? "REJECTED -> " : "ACCEPTED (unexpected) ") << msg << endl;
}

// ===== TEST 1: HAPPY PATH =====
void testHappyPath() {
    cout << "=== TEST 1: Happy path ===" << endl;
    Restaurant r("Pho 24", Location{0, 0});
    r.addMenuItem("Pho Bo", 50000);
    r.addMenuItem("Tra Sua", 30000);
    r.printMenu();

    DistanceFee fee(10000, 4000);  // 10.000 co dinh + 4.000/km
    DeliveryDriver an("An", Location{1, 1});     // cach quan ~1.41 km
    DeliveryDriver binh("Binh", Location{6, 8}); // cach quan 10 km
    DeliveryDriver* drivers[MAX_DRIVERS] = {&an, &binh};
    DriverAssigner assigner;

    Order o(1, &r, Location{3, 4}, &fee);  // khach cach quan 5 km -> phi 30.000
    o.addItem(new Topping(r.createItem(0), "Trung", 10000));      // pho + trung = 60.000
    o.addItem(new SizeUpgrade(r.createItem(1), "Large", 130));    // tra sua x1.3 = 39.000
    o.print();

    o.confirm();
    cout << "State: " << stateName(o.getState()) << endl;
    o.startPreparing();
    cout << "State: " << stateName(o.getState()) << endl;
    DeliveryDriver* d = assigner.assign(o, drivers, 2);  // chon An (gan hon Binh)
    cout << "Assigned driver: " << d->getName() << " | state: " << stateName(o.getState()) << endl;
    o.deliver();
    cout << "State: " << stateName(o.getState()) << " | " << d->getName()
         << " available again: " << (d->isAvailable() ? "yes" : "no") << endl;
    o.print();
}

// ===== TEST 2: CAC TRUONG HOP LOI =====
void testValidation() {
    cout << "=== TEST 2: Validation / error scenarios ===" << endl;
    Restaurant r("Pho 24", Location{0, 0});
    r.addMenuItem("Pho Bo", 50000);
    DistanceFee fee(10000, 4000);
    DeliveryDriver an("An", Location{1, 1});
    DeliveryDriver binh("Binh", Location{2, 2});
    DeliveryDriver* drivers[MAX_DRIVERS] = {&an, &binh};
    DriverAssigner assigner;

    Order o(2, &r, Location{3, 4}, &fee);
    o.addItem(r.createItem(0));

    // Moi khoi try/catch: thao tac sai phai bi tu choi; neu khong throw thi in "ACCEPTED (unexpected)"
    try { o.startPreparing(); printResult("Placed -> Preparing", false, ""); }
    catch (const exception& e) { printResult("Placed -> Preparing", true, e.what()); }

    o.confirm();
    try { o.confirm(); printResult("Confirm twice", false, ""); }
    catch (const exception& e) { printResult("Confirm twice", true, e.what()); }

    try { o.addItem(r.createItem(0)); printResult("Add item after confirm", false, ""); }
    catch (const exception& e) { printResult("Add item after confirm", true, e.what()); }

    o.startPreparing();
    try { o.cancel(); printResult("Cancel while Preparing", false, ""); }
    catch (const exception& e) { printResult("Cancel while Preparing", true, e.what()); }

    an.reserve();  // gia lap An da nhan don khac -> dang ban
    try { o.dispatch(&an); printResult("Dispatch busy driver (double-booking)", false, ""); }
    catch (const exception& e) { printResult("Dispatch busy driver (double-booking)", true, e.what()); }

    DeliveryDriver* d = assigner.assign(o, drivers, 2);  // An ban nen assigner phai chon Binh
    cout << "  Nearest available driver was chosen: " << d->getName() << endl;
    try { o.cancel(); printResult("Cancel while OutForDelivery", false, ""); }
    catch (const exception& e) { printResult("Cancel while OutForDelivery", true, e.what()); }

    o.deliver();
    try { o.cancel(); printResult("Cancel after Delivered", false, ""); }
    catch (const exception& e) { printResult("Cancel after Delivered", true, e.what()); }

    Order o2(3, &r, Location{1, 1}, &fee);  // don khac: huy hop le khi con o Confirmed
    o2.addItem(r.createItem(0));
    o2.confirm();
    o2.cancel();
    cout << "  Order #3 cancelled while Confirmed -> state: " << stateName(o2.getState()) << endl;
    try { o2.confirm(); printResult("Confirm a Cancelled order", false, ""); }
    catch (const exception& e) { printResult("Confirm a Cancelled order", true, e.what()); }
}

// ===== TEST 3: DA HINH =====
void testPolymorphism() {
    cout << "=== TEST 3: Polymorphism (swap FeePolicy, stack decorators) ===" << endl;
    Restaurant r("Pho 24", Location{0, 0});
    r.addMenuItem("Pho Bo", 50000);
    DistanceFee normal(10000, 4000);
    SurgeFee surge(&normal, 150);  // boc normal, nhan 1.5

    Order o(4, &r, Location{3, 4}, &normal);
    // Boc 3 lop: (50.000 + 10.000 + 15.000) * 130% = 97.500
    o.addItem(new SizeUpgrade(new Topping(new Topping(r.createItem(0), "Trung", 10000), "Bo vien", 15000), "Large", 130));
    cout << "-- With " << normal.name() << " --" << endl;
    o.print();
    long long totalNormal = o.total();

    o.setFeePolicy(&surge);  // CUNG mot don, doi policy -> ket qua doi, khong sua Order
    cout << "-- Same order, swapped to " << surge.name() << " --" << endl;
    o.print();
    cout << "Total changed: " << totalNormal << " -> " << o.total() << " VND" << endl;
}

int main() {
    testHappyPath();
    cout << endl;
    testValidation();
    cout << endl;
    testPolymorphism();
    return 0;
}
