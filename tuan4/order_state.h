#ifndef ORDER_STATE_H
#define ORDER_STATE_H

#include <string>

using namespace std;

enum OrderState {
    PLACED,
    CONFIRMED,
    PREPARING,
    OUT_FOR_DELIVERY,
    DELIVERED,
    CANCELLED
};

inline string stateName(OrderState s) {
    switch (s) {
        case PLACED: return "Placed";
        case CONFIRMED: return "Confirmed";
        case PREPARING: return "Preparing";
        case OUT_FOR_DELIVERY: return "OutForDelivery";
        case DELIVERED: return "Delivered";
        case CANCELLED: return "Cancelled";
    }
    return "Unknown";
}

#endif
