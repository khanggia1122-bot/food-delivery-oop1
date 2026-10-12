#include <iostream>
#include <string>
#include <stdexcept>
#include "location.h"
#include "menu_item.h"
#include "restaurant.h"

using namespace std;

void testMenuAndCustomization() {
    cout << "=== TUAN 1 - TEST 1: Menu va tuy bien mon (Decorator) ===" << endl;
    Restaurant r("Pho 24", Location{0, 0});
    r.addMenuItem("Pho Bo", 50000);
    r.addMenuItem("Tra Sua", 30000);
    r.printMenu();

    MenuItem* plain = r.createItem(0);
    MenuItem* withEgg = new Topping(r.createItem(0), "Trung", 10000);
    MenuItem* largeTea = new SizeUpgrade(r.createItem(1), "Large", 130);
    MenuItem* stacked = new SizeUpgrade(new Topping(new Topping(r.createItem(0), "Trung", 10000), "Bo vien", 15000), "Large", 130);

    cout << plain->getName() << ": " << plain->getPrice() << " VND" << endl;
    cout << withEgg->getName() << ": " << withEgg->getPrice() << " VND" << endl;
    cout << largeTea->getName() << ": " << largeTea->getPrice() << " VND" << endl;
    cout << stacked->getName() << ": " << stacked->getPrice() << " VND" << endl;

    delete plain;
    delete withEgg;
    delete largeTea;
    delete stacked;
}

void testRestaurantRules() {
    cout << "=== TUAN 1 - TEST 2: Quy tac cua nha hang va khoang cach ===" << endl;
    Restaurant r("Pho 24", Location{0, 0});
    r.addMenuItem("Pho Bo", 50000);

    try {
        MenuItem* x = r.createItem(5);
        delete x;
        cout << "  ACCEPTED (unexpected)" << endl;
    } catch (const exception& e) {
        cout << "  Invalid menu index: REJECTED -> " << e.what() << endl;
    }

    cout << "  Distance (0,0) -> (3,4): " << distanceKm(Location{0, 0}, Location{3, 4}) << " km" << endl;
}

int main() {
    testMenuAndCustomization();
    cout << endl;
    testRestaurantRules();
    return 0;
}
