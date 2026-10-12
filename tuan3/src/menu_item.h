#ifndef MENU_ITEM_H
#define MENU_ITEM_H

#include <string>

using namespace std;

class MenuItem {
public:
    virtual ~MenuItem() {}
    virtual string getName() const = 0;
    virtual long long getPrice() const = 0;
};

class BasicItem : public MenuItem {
private:
    string name;
    long long price;
public:
    BasicItem(const string& n, long long p) : name(n), price(p) {}
    string getName() const { return name; }
    long long getPrice() const { return price; }
};

class ItemDecorator : public MenuItem {
protected:
    MenuItem* inner;
public:
    ItemDecorator(MenuItem* i) : inner(i) {}
    virtual ~ItemDecorator() { delete inner; }
};

class Topping : public ItemDecorator {
private:
    string toppingName;
    long long extraPrice;
public:
    Topping(MenuItem* i, const string& n, long long p)
        : ItemDecorator(i), toppingName(n), extraPrice(p) {}
    string getName() const { return inner->getName() + " + " + toppingName; }
    long long getPrice() const { return inner->getPrice() + extraPrice; }
};

class SizeUpgrade : public ItemDecorator {
private:
    string sizeName;
    int percent;
public:
    SizeUpgrade(MenuItem* i, const string& n, int pct)
        : ItemDecorator(i), sizeName(n), percent(pct) {}
    string getName() const { return inner->getName() + " (" + sizeName + ")"; }
    long long getPrice() const { return inner->getPrice() * percent / 100; }
};

#endif
