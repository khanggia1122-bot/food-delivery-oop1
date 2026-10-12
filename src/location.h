#ifndef LOCATION_H
#define LOCATION_H

#include <cmath>

struct Location {
    double x;
    double y;
};

inline double distanceKm(const Location& a, const Location& b) {
    double dx = a.x - b.x;
    double dy = a.y - b.y;
    return sqrt(dx * dx + dy * dy);
}

#endif
