#ifndef ORDER_H
#define ORDER_H

#include <iostream>
#include <string>
#include <stdexcept>
#include "location.h"
#include "menu_item.h"
#include "restaurant.h"
#include "delivery_driver.h"
#include "order_state.h"
#include "fee_policy.h"

using namespace std;

const int MAX_ITEMS = 20;

class Order {
private:
    int id;
    const Restaurant* restaurant;
    Location destination;
    const FeePolicy* policy;
    MenuItem* items[MAX_ITEMS];
    int itemCount;
    OrderState state;
    DeliveryDriver* driver;

    static bool canMove(OrderState from, OrderState to) {
        static const bool table[6][6] = {
            {false, true,  false, false, false, true },
            {false, false, true,  false, false, true },
            {false, false, false, true,  false, false},
            {false, false, false, false, true,  false},
            {false, false, false, false, false, false},
            {false, false, false, false, false, false}
        };
        return table[from][to];
    }

    void moveTo(OrderState next) {
        if (!canMove(state, next)) {
            throw runtime_error("Invalid transition: " + stateName(state) + " -> " + stateName(next));
        }
        state = next;
    }

public:
    Order(int orderId, const Restaurant* r, Location dest, const FeePolicy* p)
        : id(orderId), restaurant(r), destination(dest), policy(p),
          itemCount(0), state(PLACED), driver(0) {}
    Order(const Order&) = delete;
    Order& operator=(const Order&) = delete;
    ~Order() {
        for (int i = 0; i < itemCount; i++) delete items[i];
    }

    void addItem(MenuItem* item) {
        if (state != PLACED) {
            delete item;
            throw runtime_error("Cannot add items when order is " + stateName(state));
        }
        if (itemCount >= MAX_ITEMS) {
            delete item;
            throw runtime_error("Order is full");
        }
        items[itemCount++] = item;
    }

    void setFeePolicy(const FeePolicy* p) { policy = p; }

    long long subtotal() const {
        long long sum = 0;
        for (int i = 0; i < itemCount; i++) sum += items[i]->getPrice();
        return sum;
    }
    double distance() const { return distanceKm(restaurant->getLocation(), destination); }
    long long deliveryFee() const { return policy->fee(distance()); }
    long long total() const { return subtotal() + deliveryFee(); }

    void confirm() { moveTo(CONFIRMED); }
    void startPreparing() { moveTo(PREPARING); }
    void cancel() { moveTo(CANCELLED); }

    void dispatch(DeliveryDriver* d) {
        if (d == 0) throw runtime_error("No driver given");
        if (state != PREPARING) {
            throw runtime_error("Invalid transition: " + stateName(state) + " -> " + stateName(OUT_FOR_DELIVERY));
        }
        if (!d->isAvailable()) throw runtime_error("Driver " + d->getName() + " is already busy");
        moveTo(OUT_FOR_DELIVERY);
        d->reserve();
        driver = d;
    }

    void deliver() {
        moveTo(DELIVERED);
        driver->release(destination);
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

#endif
