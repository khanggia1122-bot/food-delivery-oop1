#ifndef RESTAURANT_H
#define RESTAURANT_H

#include <iostream>
#include <string>
#include <stdexcept>
#include "location.h"
#include "menu_item.h"

using namespace std;

const int MAX_MENU = 10;

class Restaurant {
private:
    string name;
    Location location;
    MenuItem* menu[MAX_MENU];
    int menuCount;
public:
    Restaurant(const string& n, Location loc) : name(n), location(loc), menuCount(0) {}
    Restaurant(const Restaurant&) = delete;
    Restaurant& operator=(const Restaurant&) = delete;
    ~Restaurant() {
        for (int i = 0; i < menuCount; i++) delete menu[i];
    }
    void addMenuItem(const string& itemName, long long price) {
        if (menuCount >= MAX_MENU) throw runtime_error("Menu is full");
        menu[menuCount++] = new BasicItem(itemName, price);
    }
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

#endif
