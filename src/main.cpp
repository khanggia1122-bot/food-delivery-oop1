#include <iostream>
#include <string>
#include <stdexcept>
#include "location.h"
#include "menu_item.h"
#include "restaurant.h"
#include "delivery_driver.h"
#include "order_state.h"
#include "order.h"

using namespace std;

void printResult(const string& title, bool rejected, const string& msg) {
    cout << "  " << title << ": " << (rejected ? "REJECTED -> " : "ACCEPTED (unexpected) ") << msg << endl;
}

void testHappyPath() {
    cout << "=== TUAN 2 - TEST 1: Happy path (vong doi don hang) ===" << endl;
    Restaurant r("Pho 24", Location{0, 0});
    r.addMenuItem("Pho Bo", 50000);
    r.addMenuItem("Tra Sua", 30000);
    DeliveryDriver an("An", Location{1, 1});

    Order o(1, &r, Location{3, 4});
    o.addItem(new Topping(r.createItem(0), "Trung", 10000));
    o.addItem(new SizeUpgrade(r.createItem(1), "Large", 130));
    o.print();

    o.confirm();
    cout << "State: " << stateName(o.getState()) << endl;
    o.startPreparing();
    cout << "State: " << stateName(o.getState()) << endl;
    o.dispatch(&an);
    cout << "State: " << stateName(o.getState()) << " | driver: " << o.getDriver()->getName() << endl;
    o.deliver();
    cout << "State: " << stateName(o.getState()) << " | " << an.getName()
         << " available again: " << (an.isAvailable() ? "yes" : "no") << endl;
    o.print();
}

void testValidation() {
    cout << "=== TUAN 2 - TEST 2: Chuyen trang thai sai va quy tac huy ===" << endl;
    Restaurant r("Pho 24", Location{0, 0});
    r.addMenuItem("Pho Bo", 50000);
    DeliveryDriver an("An", Location{1, 1});
    DeliveryDriver binh("Binh", Location{2, 2});

    Order o(2, &r, Location{3, 4});
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

    o.dispatch(&binh);
    cout << "  Dispatched to free driver: " << o.getDriver()->getName() << endl;
    try { o.cancel(); printResult("Cancel while OutForDelivery", false, ""); }
    catch (const exception& e) { printResult("Cancel while OutForDelivery", true, e.what()); }

    o.deliver();
    try { o.cancel(); printResult("Cancel after Delivered", false, ""); }
    catch (const exception& e) { printResult("Cancel after Delivered", true, e.what()); }

    Order o2(3, &r, Location{1, 1});
    o2.addItem(r.createItem(0));
    o2.confirm();
    o2.cancel();
    cout << "  Order #3 cancelled while Confirmed -> state: " << stateName(o2.getState()) << endl;
    try { o2.confirm(); printResult("Confirm a Cancelled order", false, ""); }
    catch (const exception& e) { printResult("Confirm a Cancelled order", true, e.what()); }
}

int main() {
    testHappyPath();
    cout << endl;
    testValidation();
    return 0;
}
