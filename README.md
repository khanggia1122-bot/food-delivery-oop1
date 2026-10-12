# Online Food Delivery System

Đồ án môn OOP — HCMUTE, HK1 2026-2027 đợt 1. GVHD: Huỳnh Xuân Phụng.

Hệ thống đặt món: duyệt menu nhà hàng, đặt đơn, theo dõi đơn từ chuẩn bị đến giao hàng. Viết bằng C++11, không dùng `vector`.

## Build và chạy

```
make        # biên dịch ra ./fd
make run    # biên dịch và chạy các bộ test
```

Hoặc: `g++ -std=c++11 -Wall -Wextra -Isrc -o fd src/main.cpp && ./fd`

## Các mốc theo tuần (git tag `tuan1` … `tuan4`)

Mỗi tuần là một bản chạy được; xem lại bản của một tuần bằng `git checkout tuan2` (quay lại bằng `git checkout main`).

| Tuần | Nội dung | File mới |
|---|---|---|
| 1 | Model món và nhà hàng, tuỳ biến món bằng Decorator | `location.h`, `menu_item.h`, `restaurant.h` |
| 2 | Model `Order` và `DeliveryDriver`, vòng đời trạng thái, quy tắc hủy | `order_state.h`, `delivery_driver.h`, `order.h` |
| 3 | Phí giao hàng (Strategy), tổng tiền, gán tài xế gần nhất, test đa hình | `fee_policy.h`, `driver_assigner.h` |
| 4 | Bổ sung kiểm tra đơn rỗng, test biên, hoàn thiện báo cáo | (sửa `order.h`, `main.cpp`, `docs/`) |

## Ánh xạ yêu cầu → code

| Yêu cầu | Lớp / hàm | File |
|---|---|---|
| Model Restaurant, MenuItem, Order, DeliveryDriver | `Restaurant`, `MenuItem`/`BasicItem`, `Order`, `DeliveryDriver` | `restaurant.h`, `menu_item.h`, `order.h`, `delivery_driver.h` |
| Vòng đời trạng thái, quy tắc hủy | `Order::canMove`, `Order::moveTo` (Encapsulation) | `order.h` |
| Gán tài xế gần nhất | `DriverAssigner::assign` (Single Responsibility) | `driver_assigner.h` |
| Tổng tiền + phí giao hàng | `Order::total`, `FeePolicy` / `DistanceFee` / `SurgeFee` | `order.h`, `fee_policy.h` |
| Tuỳ biến món | `ItemDecorator` / `Topping` / `SizeUpgrade` (Decorator) | `menu_item.h` |

## Tài liệu

`docs/report.md` (báo cáo chính), `docs/giai_thich.md` (giải thích code), `docs/bao_cao_tuan_3.md` (báo cáo tuần 3), `docs/main_commented.cpp` (bản gộp một file có comment).
