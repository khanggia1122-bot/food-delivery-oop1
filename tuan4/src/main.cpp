#include <iostream>
#include <string>
#include <stdexcept>
#include "location.h"
#include "menu_item.h"
#include "restaurant.h"
#include "delivery_driver.h"
#include "order_state.h"
#include "fee_policy.h"
#include "order.h"
#include "driver_assigner.h"

using namespace std;

void printResult(const string& title, bool rejected, const string& msg) {
    cout << "  " << title << ": " << (rejected ? "REJECTED -> " : "ACCEPTED (unexpected) ") << msg << endl;
}

void testHappyPath() {
    cout << "=== TEST 1: Happy path ===" << endl;
    Restaurant r("Pho 24", Location{0, 0});
    r.addMenuItem("Pho Bo", 50000);
    r.addMenuItem("Tra Sua", 30000);
    r.printMenu();

    DistanceFee fee(10000, 4000);
    DeliveryDriver an("An", Location{1, 1});
    DeliveryDriver binh("Binh", Location{6, 8});
    DeliveryDriver* drivers[MAX_DRIVERS] = {&an, &binh};
    DriverAssigner assigner;

    Order o(1, &r, Location{3, 4}, &fee);
    o.addItem(new Topping(r.createItem(0), "Trung", 10000));
    o.addItem(new SizeUpgrade(r.createItem(1), "Large", 130));
    o.print();

    o.confirm();
    cout << "State: " << stateName(o.getState()) << endl;
    o.startPreparing();
    cout << "State: " << stateName(o.getState()) << endl;
    DeliveryDriver* d = assigner.assign(o, drivers, 2);
    cout << "Assigned driver: " << d->getName() << " | state: " << stateName(o.getState()) << endl;
    o.deliver();
    cout << "State: " << stateName(o.getState()) << " | " << d->getName()
         << " available again: " << (d->isAvailable() ? "yes" : "no") << endl;
    o.print();
}

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

    an.reserve();
    try { o.dispatch(&an); printResult("Dispatch busy driver (double-booking)", false, ""); }
    catch (const exception& e) { printResult("Dispatch busy driver (double-booking)", true, e.what()); }

    DeliveryDriver* d = assigner.assign(o, drivers, 2);
    cout << "  Nearest available driver was chosen: " << d->getName() << endl;
    try { o.cancel(); printResult("Cancel while OutForDelivery", false, ""); }
    catch (const exception& e) { printResult("Cancel while OutForDelivery", true, e.what()); }

    o.deliver();
    try { o.cancel(); printResult("Cancel after Delivered", false, ""); }
    catch (const exception& e) { printResult("Cancel after Delivered", true, e.what()); }

    Order o2(3, &r, Location{1, 1}, &fee);
    o2.addItem(r.createItem(0));
    o2.confirm();
    o2.cancel();
    cout << "  Order #3 cancelled while Confirmed -> state: " << stateName(o2.getState()) << endl;
    try { o2.confirm(); printResult("Confirm a Cancelled order", false, ""); }
    catch (const exception& e) { printResult("Confirm a Cancelled order", true, e.what()); }
}

void testPolymorphism() {
    cout << "=== TEST 3: Polymorphism (swap FeePolicy, stack decorators) ===" << endl;
    Restaurant r("Pho 24", Location{0, 0});
    r.addMenuItem("Pho Bo", 50000);
    DistanceFee normal(10000, 4000);
    SurgeFee surge(&normal, 150);

    Order o(4, &r, Location{3, 4}, &normal);
    o.addItem(new SizeUpgrade(new Topping(new Topping(r.createItem(0), "Trung", 10000), "Bo vien", 15000), "Large", 130));
    cout << "-- With " << normal.name() << " --" << endl;
    o.print();
    long long totalNormal = o.total();

    o.setFeePolicy(&surge);
    cout << "-- Same order, swapped to " << surge.name() << " --" << endl;
    o.print();
    cout << "Total changed: " << totalNormal << " -> " << o.total() << " VND" << endl;
}

void testEdgeCases() {
    cout << "=== TEST 4: Edge cases ===" << endl;
    Restaurant r("Pho 24", Location{0, 0});
    r.addMenuItem("Pho Bo", 50000);
    DistanceFee fee(10000, 4000);
    DriverAssigner assigner;

    Order empty(5, &r, Location{3, 4}, &fee);
    try { empty.confirm(); printResult("Confirm an empty order", false, ""); }
    catch (const exception& e) { printResult("Confirm an empty order", true, e.what()); }

    try { r.createItem(-1); printResult("Invalid menu index", false, ""); }
    catch (const exception& e) { printResult("Invalid menu index", true, e.what()); }

    Restaurant big("Big", Location{0, 0});
    for (int i = 0; i < MAX_MENU; i++) big.addMenuItem("Item", 1000);
    try { big.addMenuItem("Extra", 1000); printResult("Add item to full menu", false, ""); }
    catch (const exception& e) { printResult("Add item to full menu", true, e.what()); }

    Order full(6, &r, Location{3, 4}, &fee);
    for (int i = 0; i < MAX_ITEMS; i++) full.addItem(r.createItem(0));
    try { full.addItem(r.createItem(0)); printResult("Add item to full order", false, ""); }
    catch (const exception& e) { printResult("Add item to full order", true, e.what()); }

    DeliveryDriver an("An", Location{1, 1});
    an.reserve();
    DeliveryDriver* drivers[MAX_DRIVERS] = {&an};
    Order o(7, &r, Location{3, 4}, &fee);
    o.addItem(r.createItem(0));
    o.confirm();
    o.startPreparing();
    try { assigner.assign(o, drivers, 1); printResult("No available driver", false, ""); }
    catch (const exception& e) { printResult("No available driver", true, e.what()); }
    cout << "  Order state unchanged after failed assignment: " << stateName(o.getState()) << endl;
}

int main() {
    testHappyPath();
    cout << endl;
    testValidation();
    cout << endl;
    testPolymorphism();
    cout << endl;
    testEdgeCases();
    return 0;
}
