#ifndef FEE_POLICY_H
#define FEE_POLICY_H

#include <string>

using namespace std;

class FeePolicy {
public:
    virtual ~FeePolicy() {}
    virtual long long fee(double km) const = 0;
    virtual string name() const = 0;
};

class DistanceFee : public FeePolicy {
private:
    long long baseFee;
    long long perKm;
public:
    DistanceFee(long long b, long long p) : baseFee(b), perKm(p) {}
    long long fee(double km) const { return baseFee + (long long)(perKm * km + 0.5); }
    string name() const { return "DistanceFee"; }
};

class SurgeFee : public FeePolicy {
private:
    const FeePolicy* inner;
    int percent;
public:
    SurgeFee(const FeePolicy* i, int pct) : inner(i), percent(pct) {}
    long long fee(double km) const { return inner->fee(km) * percent / 100; }
    string name() const { return "SurgeFee(" + inner->name() + " x" + to_string(percent) + "%)"; }
};

#endif
