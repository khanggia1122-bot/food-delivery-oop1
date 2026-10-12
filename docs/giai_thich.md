# Giải thích code Online Food Delivery System

> **Lưu ý về vị trí code:** code được chia thành nhiều file header trong `src/`. Mục 1 ở dưới nằm ở `main.cpp` và các header; mục 2 → `location.h`; mục 3 → `order_state.h`; mục 4 → `menu_item.h`; mục 5 → `restaurant.h`; mục 6 → `fee_policy.h`; mục 7 → `delivery_driver.h`; mục 8 → `order.h`; mục 9 → `driver_assigner.h`; mục 10 → `main.cpp`. Nội dung từng lớp không đổi. File `docs/main_commented.cpp` là bản gộp một file có comment, dùng để đọc.

Giải thích theo thứ tự trong `main.cpp`. Phần lặp lại (getter, constructor đơn giản) được gom lại, phần quan trọng giải thích từng dòng.

## 1. Header và hằng số

| Dòng | Giải thích |
|---|---|
| `#include <iostream>` | Dùng `cout`, `endl` để in. |
| `#include <string>` | Dùng `string` và `to_string`. |
| `#include <cmath>` | Dùng `sqrt` tính khoảng cách. |
| `#include <stdexcept>` | Dùng `runtime_error` để báo lỗi khi thao tác không hợp lệ. |
| `using namespace std;` | Khỏi viết `std::` trước mỗi tên. |
| `const int MAX_MENU / MAX_ITEMS / MAX_DRIVERS` | Dùng mảng thô nên phải cố định kích thước tối đa. |

## 2. `Location` và `distanceKm`

- `struct Location { double x; double y; }`: một điểm trên bản đồ, đơn vị km.
- `distanceKm`: tính `dx = a.x - b.x`, `dy = a.y - b.y` rồi trả về `sqrt(dx*dx + dy*dy)` (khoảng cách Euclid). Truyền `const Location&` để không sao chép và không bị sửa.

## 3. `OrderState` và `stateName`

- `enum OrderState { PLACED, CONFIRMED, ... }`: enum tự đánh số 0 đến 5 theo thứ tự khai báo. Các số này dùng làm chỉ số cho bảng chuyển trạng thái, nên thứ tự enum phải khớp thứ tự hàng/cột của bảng.
- `stateName`: `switch` đổi enum thành chuỗi để in. `return "Unknown"` ở cuối chỉ để trình biên dịch không cảnh báo.

## 4. `MenuItem` và Decorator

**`MenuItem` (lớp trừu tượng)**
- `virtual ~MenuItem() {}`: destructor ảo. Khi `delete` qua con trỏ `MenuItem*`, destructor của lớp con vẫn được gọi đúng.
- `virtual string getName() const = 0;` và `virtual long long getPrice() const = 0;`: `= 0` là hàm thuần ảo, lớp con bắt buộc cài đặt. `const` vì hàm chỉ đọc.

**`BasicItem`**: giữ `name`, `price`; hai getter trả lại chính chúng. Đây là lõi trong cùng của chuỗi bọc.

**`ItemDecorator`**
- `class ItemDecorator : public MenuItem`: "là một" MenuItem, nên có thể đặt vào bất cứ chỗ nào cần MenuItem.
- `MenuItem* inner;`: "có một" MenuItem bên trong. Kết hợp hai dòng này là Decorator: bọc được lồng nhau nhiều lớp.
- `~ItemDecorator() { delete inner; }`: huỷ lớp ngoài thì huỷ luôn cả chuỗi bên trong, không rò bộ nhớ.

**`Topping`**
- Constructor: `ItemDecorator(i)` khởi tạo phần cha, rồi lưu `toppingName`, `extraPrice`.
- `getName()`: `inner->getName() + " + " + toppingName`, tức tên món bên trong cộng thêm tên topping.
- `getPrice()`: `inner->getPrice() + extraPrice`. Gọi đệ quy xuống lớp trong, nên dù bọc bao nhiêu lớp vẫn tính đúng.
- Tên và giá topping là tham số, nên một lớp phục vụ mọi topping.

**`SizeUpgrade`**: giống `Topping`, nhưng `getPrice()` = `inner->getPrice() * percent / 100` (130 nghĩa là tăng 30%). Thứ tự bọc quan trọng: `SizeUpgrade` bọc ngoài cùng thì nhân luôn cả phần topping.

## 5. `Restaurant`

- `MenuItem* menu[MAX_MENU]; int menuCount;`: mảng thô các con trỏ món và số món hiện có.
- `Restaurant(const Restaurant&) = delete;` và `operator=`: cấm sao chép. Nếu sao chép, hai đối tượng sẽ cùng `delete` một con trỏ và gây lỗi double delete.
- Destructor: vòng `for` `delete menu[i]` để giải phóng từng món.
- `addMenuItem`: nếu đầy thì `throw`; không thì `menu[menuCount++] = new BasicItem(...)` (tạo món rồi tăng đếm).
- `createItem(index)`: kiểm tra chỉ số hợp lệ, rồi trả về **bản sao mới** của món. Mỗi đơn tuỳ biến bản sao của riêng nó, không đụng vào menu gốc, và `Order` có thể `delete` thoải mái.
- `printMenu`: duyệt mảng và in.

## 6. `FeePolicy` (Strategy)

- `FeePolicy`: giao diện gồm `fee(km)` và `name()`.
- `DistanceFee::fee`: `baseFee + (long long)(perKm * km + 0.5)`. Ép kiểu về số nguyên thì C++ cắt phần thập phân, nên cộng 0.5 trước để thành làm tròn.
- `SurgeFee`: giữ `const FeePolicy* inner` (không sở hữu, chỉ trỏ tới policy có sẵn) và `percent`. `fee` = `inner->fee(km) * percent / 100`, tức nhân hệ số lên phí gốc. `name()` ghép tên policy bên trong để in ra rõ.

## 7. `DeliveryDriver`

- `available`: true nghĩa là đang rảnh.
- `reserve()`: nếu `!available` thì `throw` (chống double-booking); không thì đặt `available = false`.
- `release(newLocation)`: trả về rảnh và cập nhật vị trí tài xế tại nơi giao (lần gán sau tính khoảng cách từ vị trí mới).

## 8. `Order` (phần quan trọng nhất)

**Thuộc tính private**
- `restaurant`, `policy`: con trỏ `const`, `Order` không sở hữu.
- `items[MAX_ITEMS]`: `Order` sở hữu các món đã tuỳ biến, nên destructor `delete` từng món.
- `state`: **private**, đây là chìa khoá của Encapsulation. Không có hàm `setState` công khai.
- `driver`: `0` nghĩa là chưa gán.

**`canMove(from, to)`**
- `static`: không cần đối tượng, chỉ tra bảng.
- `table[6][6]`: hàng là trạng thái hiện tại, cột là trạng thái muốn tới, cùng thứ tự enum.
  - Hàng Placed: `true` ở Confirmed và Cancelled.
  - Hàng Confirmed: `true` ở Preparing và Cancelled.
  - Hàng Preparing: chỉ `true` ở OutForDelivery (**không có Cancelled**, đây là luật hủy).
  - Hàng OutForDelivery: chỉ `true` ở Delivered.
  - Hàng Delivered và Cancelled: toàn `false`, là trạng thái kết thúc.
- `return table[from][to];`: enum tự chuyển thành số nguyên để làm chỉ số.

**`moveTo(next)`**: nếu `!canMove(state, next)` thì `throw` với thông báo "Invalid transition: A -> B"; không thì `state = next`. Mọi thay đổi trạng thái trong lớp đều đi qua đây, nên luật chỉ nằm ở một chỗ.

**`addItem`**: nếu `state != PLACED` hoặc đơn đầy thì `delete item` rồi `throw`. Phải `delete` trước vì `Order` đã nhận quyền sở hữu ngay lúc được gọi; không `delete` sẽ rò bộ nhớ.

**Tính tiền**: `subtotal()` cộng `items[i]->getPrice()` (gọi ảo, mỗi món tự tính, kể cả decorator). `deliveryFee()` gọi `policy->fee(distance())` mà không cần biết policy cụ thể. `total()` = `subtotal() + deliveryFee()`.

**`confirm`, `startPreparing`, `cancel`**: mỗi hàm chỉ gọi `moveTo(...)`. `cancel()` không có `if` riêng, bảng quyết định có được hủy hay không.

**`dispatch(d)`**
1. `d == 0`: không có tài xế thì báo lỗi.
2. `state != PREPARING`: sai trạng thái thì báo lỗi "Invalid transition" (kiểm tra trước để báo đúng nguyên nhân).
3. `!d->isAvailable()`: tài xế bận thì báo lỗi (double-booking).
4. `moveTo(OUT_FOR_DELIVERY)`: chuyển trạng thái.
5. `d->reserve()`: đánh dấu tài xế bận. Ba kiểm tra ở trên đã đảm bảo bước này không lỗi, nên không bao giờ rơi vào tình trạng trạng thái đổi nhưng tài xế vẫn rảnh.
6. `driver = d;`: lưu tài xế.

**`deliver`**: `moveTo(DELIVERED)` trước (sai trạng thái thì ném lỗi ngay, chưa đụng tài xế), rồi `driver->release(destination)`.

## 9. `DriverAssigner` (Single Responsibility)

- `from = order.getRestaurant()->getLocation()`: tài xế phải tới quán, nên tính khoảng cách từ quán.
- Vòng `for`: bỏ qua tài xế bận (`continue`); với tài xế rảnh tính khoảng cách `d`; nếu là người đầu tiên (`best == 0`) hoặc gần hơn (`d < bestDist`) thì ghi nhận.
- `best == 0` sau vòng lặp: không ai rảnh thì `throw`.
- `order.dispatch(best)`: **không tự đổi trạng thái**, nhờ `Order` quyết định. Lớp này không biết luật trạng thái, `Order` không biết cách chọn tài xế.

## 10. Hàm test

- `printResult(title, rejected, msg)`: in "REJECTED" nếu bị từ chối đúng, "ACCEPTED (unexpected)" nếu lẽ ra phải bị từ chối nhưng không.
- **Test 1**: tạo quán, 2 món, 2 tài xế (An cách ~1.41 km, Bình cách 10 km), `DriverAssigner`. Đơn giao cách 5 km nên phí = 10.000 + 4.000×5 = 30.000. Phở + trứng = 60.000; trà sữa Large = 30.000×1.3 = 39.000; tổng = 99.000 + 30.000 = 129.000. Đi hết vòng đời, `assign` chọn An; sau `deliver`, An rảnh lại.
- **Test 2**: mỗi khối `try`/`catch` thử một thao tác sai. `an.reserve()` giả lập An đang bận, nên `dispatch(&an)` bị từ chối còn `assign` phải chọn Bình. Cuối cùng chứng minh hủy hợp lệ ở Confirmed (đơn #3), nhưng không confirm lại được đơn đã hủy.
- **Test 3**: cùng một đơn, gọi `setFeePolicy(&surge)` để đổi policy. Phí 30.000 thành 45.000, tổng 127.500 thành 142.500. Decorator ba lớp: (50.000 + 10.000 + 15.000) × 130% = 97.500.

## 11. Câu hỏi hay gặp khi bảo vệ

1. **Vì sao `state` private?** Để chỉ `Order` mới được đổi trạng thái. Nếu public, ai cũng có thể gán `state = DELIVERED` và phá luật vòng đời.
2. **Vì sao không cho hủy khi Preparing?** Quán đã dùng nguyên liệu, công sức không thu hồi được. Sau đó món đã rời quán hoặc đã giao. Trong code, bảng ở cột Cancelled chỉ có `true` cho Placed và Confirmed.
3. **Vì sao dùng Decorator thay vì kế thừa?** Kế thừa sẽ cần một lớp cho mỗi tổ hợp (PhoTrung, PhoTrungBoVien, PhoTrungLarge, ...). Decorator chỉ cần `Topping` và `SizeUpgrade`, bọc tuỳ ý.
4. **Vì sao tách `DriverAssigner` khỏi `Order`?** Single Responsibility: đổi cách chọn tài xế không ảnh hưởng luật trạng thái và ngược lại.
5. **Vì sao `createItem` trả về bản sao?** Để mỗi đơn có món riêng, `Order` sở hữu và `delete` mà không làm hỏng menu của quán.
6. **Nếu muốn thêm trạng thái mới?** Thêm vào enum, mở rộng bảng `table` và `stateName`; các hàm khác không phải sửa.
