#ifndef DELIVERY_DRIVER_H
#define DELIVERY_DRIVER_H

#include <string>
#include <stdexcept>
#include "location.h"

using namespace std;

const int MAX_DRIVERS = 10;

class DeliveryDriver {
private:
    string name;
    Location location;
    bool available;
public:
    DeliveryDriver(const string& n, Location loc) : name(n), location(loc), available(true) {}
    const string& getName() const { return name; }
    const Location& getLocation() const { return location; }
    bool isAvailable() const { return available; }
    void reserve() {
        if (!available) throw runtime_error("Driver " + name + " is already busy");
        available = false;
    }
    void release(const Location& newLocation) {
        available = true;
        location = newLocation;
    }
};

#endif
