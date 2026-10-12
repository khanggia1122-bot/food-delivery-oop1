# Online Food Delivery System — Báo cáo

Môn: OOP — HK1 2026-2027 đợt 1 · GVHD: Huỳnh Xuân Phụng

GitHub: [DIEN_LINK_REPO_CUA_BAN]

## 1. Build / Run

```
make   # hoặc: g++ -std=c++11 -Wall -Wextra -Isrc -o fd src/main.cpp
./fd
```

## 2. Phát biểu lại bài toán

Xây dựng hệ thống đặt món: duyệt menu nhà hàng, đặt đơn, theo dõi đơn qua các trạng thái chuẩn bị và giao hàng. Hệ thống gồm `Restaurant`, `MenuItem`, `Order`, `DeliveryDriver`; đơn có vòng đời trạng thái hợp lệ, gán tài xế gần nhất còn rảnh, tính tổng tiền gồm phí giao hàng (theo khoảng cách hoặc surge), và tùy biến món bằng composition.

## 3. Sơ đồ trạng thái

```
 Placed ──► Confirmed ──► Preparing ──► OutForDelivery ──► Delivered
   │            │
   └────────────┴──────► Cancelled
```

Delivered và Cancelled là trạng thái kết thúc. Chỉ cho phép hủy ở Placed và Confirmed.

## 4. Sơ đồ lớp

```
MenuItem (abstract)  <|-- BasicItem
   ^                 <|-- ItemDecorator (có-một MenuItem inner)
   |                          <|-- Topping
   |                          <|-- SizeUpgrade
Restaurant  ◆── MenuItem[MAX_MENU]
Order       ◆── MenuItem*[MAX_ITEMS]   (sở hữu các món đã tùy biến)
Order       ──► Restaurant, DeliveryDriver, FeePolicy
FeePolicy (abstract) <|-- DistanceFee
                     <|-- SurgeFee (có-một FeePolicy inner)
DriverAssigner ──► Order (qua public API), DeliveryDriver[]
```

## 5. Self-Explanation

### 5.1 Model Restaurant, MenuItem, Order, DeliveryDriver
Mỗi lớp giữ đúng dữ liệu của nó: `Restaurant` giữ menu và vị trí, `DeliveryDriver` giữ vị trí và cờ `available`, `Order` giữ danh sách món, trạng thái, tài xế. Tất cả thuộc tính đều `private`.

### 5.2 Vòng đời trạng thái — Encapsulation (`Order::canMove`, `Order::moveTo`)
`state` là `private`, bên ngoài chỉ gọi `confirm()`, `startPreparing()`, `dispatch()`, `deliver()`, `cancel()`. Mọi hàm đều đi qua `moveTo()`, hàm này tra bảng `table[6][6]` trong `canMove()` và ném `runtime_error` nếu bước đi không hợp lệ. Vì không có `setState()` công khai nên không đoạn code nào ngoài `Order` có thể nhảy trạng thái bất hợp lệ, quy tắc nằm đúng một chỗ, muốn đổi luật chỉ sửa bảng.

### 5.3 Quy tắc hủy đơn
Bảng chỉ có hai ô `true` dẫn tới Cancelled: từ Placed và Confirmed. Khi chuyển sang Preparing, nhà hàng đã bắt đầu dùng nguyên liệu và công sức nấu, nên hủy lúc này gây thiệt hại không thu hồi được; từ OutForDelivery đã có tài xế và món đã rời quán. Trong code, không cần `if` riêng cho hủy: `cancel()` chỉ gọi `moveTo(CANCELLED)`, và các hàng Preparing, OutForDelivery, Delivered, Cancelled của bảng đều `false` ở cột Cancelled. Test 2 chứng minh việc này.

### 5.4 Gán tài xế — Single Responsibility (`DriverAssigner::assign` vs `Order`)
`DriverAssigner` chỉ lo việc chọn tài xế gần nhất còn rảnh (duyệt mảng, so `distanceKm`). `Order` chỉ lo trạng thái và kiểm tra hợp lệ trong `dispatch()`. `assign()` không đụng vào `state`, nó gọi `order.dispatch(best)` và để `Order` tự quyết định có chấp nhận hay không. Nhờ vậy, đổi thuật toán chọn (ví dụ theo rating) chỉ sửa `DriverAssigner`, còn luật trạng thái không bị ảnh hưởng.

### 5.5 Tổng tiền và phí giao hàng — Strategy (`FeePolicy`, `DistanceFee`, `SurgeFee`)
`Order::total()` = `subtotal()` + `deliveryFee()`, trong đó `deliveryFee()` gọi `policy->fee(distance())` mà không biết policy cụ thể. `SurgeFee` bọc một `FeePolicy` khác và nhân hệ số, nên surge là phép cộng dồn lên phí khoảng cách chứ không phải bản sao code. Đổi chính sách bằng `setFeePolicy()` là đổi kết quả mà không sửa `Order`.

### 5.6 Tùy biến món — Decorator/Composition (`ItemDecorator`, `Topping`, `SizeUpgrade`)
Mỗi món tùy biến là một chuỗi bọc: `SizeUpgrade(Topping(Topping(BasicItem)))`. `Topping` nhận tên và giá làm tham số nên một lớp phục vụ vô số topping, tránh bùng nổ lớp kiểu `PhoTrungBoVienLarge`. `getPrice()` và `getName()` đệ quy qua `inner`; `ItemDecorator` giữ `delete inner` trong destructor nên huỷ một lớp là huỷ cả chuỗi. Thứ tự bọc ảnh hưởng giá (Large nhân sau khi đã cộng topping), đây là hành vi mong muốn và được thể hiện trong Test 3.

## 6. Test evidence

Output thực tế của `./fd`:

```
=== TEST 1: Happy path ===
Menu of Pho 24:
  [0] Pho Bo - 50000 VND
  [1] Tra Sua - 30000 VND
Order #1 from Pho 24 | state: Placed
  - Pho Bo + Trung: 60000 VND
  - Tra Sua (Large): 39000 VND
  Subtotal: 99000 VND
  Distance: 5 km, fee (DistanceFee): 30000 VND
  TOTAL: 129000 VND
State: Confirmed
State: Preparing
Assigned driver: An | state: OutForDelivery
State: Delivered | An available again: yes
Order #1 from Pho 24 | state: Delivered
  - Pho Bo + Trung: 60000 VND
  - Tra Sua (Large): 39000 VND
  Subtotal: 99000 VND
  Distance: 5 km, fee (DistanceFee): 30000 VND
  TOTAL: 129000 VND
  Driver: An

=== TEST 2: Validation / error scenarios ===
  Placed -> Preparing: REJECTED -> Invalid transition: Placed -> Preparing
  Confirm twice: REJECTED -> Invalid transition: Confirmed -> Confirmed
  Add item after confirm: REJECTED -> Cannot add items when order is Confirmed
  Cancel while Preparing: REJECTED -> Invalid transition: Preparing -> Cancelled
  Dispatch busy driver (double-booking): REJECTED -> Driver An is already busy
  Nearest available driver was chosen: Binh
  Cancel while OutForDelivery: REJECTED -> Invalid transition: OutForDelivery -> Cancelled
  Cancel after Delivered: REJECTED -> Invalid transition: Delivered -> Cancelled
  Order #3 cancelled while Confirmed -> state: Cancelled
  Confirm a Cancelled order: REJECTED -> Invalid transition: Cancelled -> Confirmed

=== TEST 3: Polymorphism (swap FeePolicy, stack decorators) ===
-- With DistanceFee --
Order #4 from Pho 24 | state: Placed
  - Pho Bo + Trung + Bo vien (Large): 97500 VND
  Subtotal: 97500 VND
  Distance: 5 km, fee (DistanceFee): 30000 VND
  TOTAL: 127500 VND
-- Same order, swapped to SurgeFee(DistanceFee x150%) --
Order #4 from Pho 24 | state: Placed
  - Pho Bo + Trung + Bo vien (Large): 97500 VND
  Subtotal: 97500 VND
  Distance: 5 km, fee (SurgeFee(DistanceFee x150%)): 45000 VND
  TOTAL: 142500 VND
Total changed: 127500 -> 142500 VND
```

- Test 1: happy path Placed → Confirmed → Preparing → OutForDelivery → Delivered, gán tài xế An (gần quán nhất), An được trả về trạng thái rảnh.
- Test 2: từ chối chuyển trạng thái sai, thêm món sau khi xác nhận, hủy khi Preparing / OutForDelivery / Delivered, double-booking tài xế bận.
- Test 3: đổi `DistanceFee` sang `SurgeFee` làm tổng đổi 127500 → 142500; chồng nhiều decorator.

## 7. AI Usage Log

| Công cụ | Dùng cho | Phần tôi chỉnh / kiểm chứng |
|---|---|---|
| Claude | Gợi ý khung thiết kế và sinh bản code ban đầu | (điền: phần bạn tự đọc, sửa, chạy lại và hiểu) |
