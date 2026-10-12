# Báo cáo tiến độ tuần 3 — Online Food Delivery System

- Môn: OOP — HK1 2026-2027 đợt 1 · GVHD: Huỳnh Xuân Phụng
- Sinh viên: Khang · MSSV: [DIEN_MSSV]
- Repository GitHub: [DIEN_LINK_REPO]
- Thời gian báo cáo: tuần 3 ([DIEN_NGAY_BAT_DAU] – [DIEN_NGAY_KET_THUC])

## 1. Mục tiêu của tuần

Hoàn thành bản cài đặt đầu tiên chạy được của cả 5 yêu cầu chức năng trong đề (model, vòng đời trạng thái, gán tài xế, tính tổng tiền, tuỳ biến món) và có test chứng minh, kèm bản nháp báo cáo.

## 2. Công việc đã thực hiện

1. **Phân tích đề và thiết kế lớp.** Xác định 4 lớp model (`Restaurant`, `MenuItem`, `Order`, `DeliveryDriver`) và các lớp hỗ trợ: `FeePolicy` (strategy phí), `ItemDecorator` (tuỳ biến món), `DriverAssigner` (gán tài xế). Vẽ sơ đồ trạng thái và sơ đồ lớp dạng ASCII (trong `docs/report.md`).
2. **Cài đặt vòng đời đơn hàng.** `Order` giữ `state` ở chế độ private; mọi chuyển trạng thái đi qua `moveTo()` tra bảng `canMove[6][6]`. Chỉ cho hủy ở Placed và Confirmed; từ Preparing trở đi bị từ chối.
3. **Cài đặt gán tài xế.** `DriverAssigner::assign` chọn tài xế rảnh gần quán nhất rồi gọi `Order::dispatch`. Việc kiểm tra tài xế bận và trạng thái đơn nằm trong `dispatch`, nên không thể gán trùng tài xế (double-booking).
4. **Cài đặt tính tiền.** `Order::total()` = tổng giá món + phí giao hàng. Phí tính qua `DistanceFee` (10.000 + 4.000/km) hoặc `SurgeFee` (nhân hệ số lên phí khoảng cách), đổi được lúc chạy bằng `setFeePolicy`.
5. **Cài đặt tuỳ biến món.** Dùng Decorator: `Topping` và `SizeUpgrade` bọc `BasicItem`, bọc lồng nhau được, không tạo lớp con cho từng món.
6. **Viết test trong `main()`** (3 bộ, chạy bằng `make run`) và đối chiếu kết quả với số liệu tính tay.
7. **Viết tài liệu:** báo cáo (`docs/report.md`), giải thích từng phần code (`docs/giai_thich.md`) và bản code có comment để chuẩn bị bảo vệ.
8. **Tạo repository GitHub và đẩy mã nguồn, tài liệu lên** (theo yêu cầu bổ sung của GVHD), mỗi commit tương ứng một nhóm nội dung. [XOA DONG NAY NEU CHUA LAM; chi giu khi da push va co link o dau bao cao]

## 3. Kết quả đạt được

| Yêu cầu | Trạng thái | Bằng chứng |
|---|---|---|
| Model 4 lớp | Xong | `src/main.cpp` |
| Vòng đời + quy tắc hủy | Xong | Test 2: 8 thao tác sai đều bị từ chối đúng |
| Gán tài xế gần nhất | Xong | Test 1 chọn An (≈1,41 km) thay vì Bình (10 km); Test 2 bỏ qua tài xế đang bận |
| Tổng tiền + phí | Xong | Test 1: 99.000 + 30.000 = 129.000 VND |
| Tuỳ biến món (composition) | Xong | Test 3: chồng 3 lớp, giá 97.500 VND |
| Đa hình (đổi policy) | Xong | Test 3: tổng đổi 127.500 → 142.500 VND khi chuyển sang `SurgeFee` |

Chương trình biên dịch không cảnh báo với `g++ -std=c++11 -Wall -Wextra`; toàn bộ output test nằm trong `docs/report.md`, mục 6.

**Các quyết định thiết kế và vấn đề đã xử lý**
- Trong `dispatch()`, kiểm tra trạng thái và tài xế bận được làm **trước** khi đổi trạng thái, để tránh tình huống đơn đã sang OutForDelivery nhưng tài xế không được đánh dấu bận.
- `Order::addItem` phải `delete` món trước khi `throw`, vì `Order` đã nhận quyền sở hữu; nếu không sẽ rò bộ nhớ.
- `Restaurant` và `Order` cấm sao chép (`= delete`) để tránh `delete` hai lần trên cùng con trỏ.
- `createItem` trả về bản sao món để mỗi đơn tuỳ biến độc lập với menu gốc.

**Hạn chế hiện tại:** dùng mảng kích thước cố định (`MAX_MENU`, `MAX_ITEMS`, `MAX_DRIVERS`) theo yêu cầu không dùng `vector`; chưa có giao diện nhập liệu, dữ liệu test được đặt sẵn trong code; chưa có kiểm thử tự động bằng framework.

## 4. Sử dụng AI (tóm tắt, chi tiết ở AI Usage Log trong `docs/report.md`)

Dùng Claude để gợi ý khung thiết kế, sinh bản code ban đầu và bản nháp tài liệu. Tôi đã [DIEN: những gì bạn thực sự tự đọc, chạy lại, sửa và hiểu — viết đúng sự thật].

## 5. Kế hoạch tuần 4

1. Hoàn thiện `docs/report.md`: điền đầy đủ AI Usage Log, rà lại phần Self-Explanation bằng lời của mình.
2. Bổ sung thêm test biên: đơn rỗng, menu đầy, đơn đầy, không còn tài xế rảnh.
3. Rà soát code theo từng câu hỏi trong `docs/giai_thich.md` mục 11 và luyện trả lời để chuẩn bị bảo vệ trực tiếp.
4. Giữ lịch sử commit rõ ràng cho từng thay đổi, nộp lại link repo và báo cáo cho GVHD.
