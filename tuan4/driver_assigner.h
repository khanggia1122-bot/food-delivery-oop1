#ifndef DRIVER_ASSIGNER_H
#define DRIVER_ASSIGNER_H

#include <stdexcept>
#include "location.h"
#include "delivery_driver.h"
#include "order.h"

using namespace std;

class DriverAssigner {
public:
    DeliveryDriver* assign(Order& order, DeliveryDriver** drivers, int n) const {
        const Location& from = order.getRestaurant()->getLocation();
        DeliveryDriver* best = 0;
        double bestDist = 0;
        for (int i = 0; i < n; i++) {
            if (!drivers[i]->isAvailable()) continue;
            double d = distanceKm(from, drivers[i]->getLocation());
            if (best == 0 || d < bestDist) {
                best = drivers[i];
                bestDist = d;
            }
        }
        if (best == 0) throw runtime_error("No available driver");
        order.dispatch(best);
        return best;
    }
};

#endif
