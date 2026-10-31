# Gỡ cài đặt an toàn — EasyMount 0.3.5

Gỡ từ Windows Settings hoặc chạy bộ Setup và chọn Uninstall. Bộ MSI cũng có cùng bước kiểm tra khi được gọi trực tiếp, kể cả chế độ im lặng.

Trước khi Windows Installer kiểm tra file đang dùng và xóa ứng dụng, helper yêu cầu các cửa sổ EasyMount của bản cài ngắt ổ rồi thoát. Các phiên còn sống của thư mục dữ liệu đã đăng ký được phục hồi bằng xác thực cũ và ngắt qua cùng kiểm tra upload của app; không tự kết nối các profile đã ngắt.

Nếu còn file đang upload/chờ upload, lỗi upload, cửa sổ cấu hình đang mở, thao tác đang chạy, phiên không xác minh được hoặc worker trong thư mục cài chưa được quản lý, helper trả lỗi và MSI dừng trước khi xóa app. Không End Task để bỏ qua: hoàn tất upload, đóng cửa sổ cấu hình rồi gỡ lại.

App sử dụng named pipe chỉ cho cùng người dùng Windows. Khi Windows Installer đang xử lý gỡ, marker cùng mutex Windows Installer chặn mở/kết nối mới. Nếu phiên thuộc tài khoản Windows khác hoặc không truy cập được, bộ gỡ dừng thay vì giết tiến trình. Chi tiết lỗi gần nhất có thể nằm tại `%LOCALAPPDATA%\EasyMount\uninstall-blocked.txt`.

WinFsp là driver dùng chung, vẫn được giữ. Cấu hình tài khoản, profile, cache và dữ liệu cloud không bị xóa. Danh sách thư mục dữ liệu runtime nằm trong `%LOCALAPPDATA%\EasyMount\runtime-roots.json`; file này chỉ lưu đường dẫn cục bộ, không chứa token.

Tính năng có từ 0.3.5. Phiên custom-data-dir từ bản cũ cần được mở lại bằng bản mới để đăng ký đường dẫn; worker không xác minh được sẽ không bị cưỡng bức ngắt. Khi nâng từ bản cũ hơn, hãy thoát app an toàn trước khi cập nhật.

## Kiểm chứng thực tế (31/10/2026)

- 65 kiểm tra backend/mount Windows thật đạt, gồm chặn gỡ khi session không xác minh được hoặc còn file chờ.
- MSI trực tiếp `/x /qn` trả 1603 với file thử đang chờ, giữ app, ổ và cache.
- Thử lại khi app đã dừng nhưng worker còn sống: tiếp tục bị chặn, không mất cache.
- Sau khi backend local nhận đủ file thử, helper nhận lại và gỡ được phiên mồ côi.
- Gỡ bằng Setup khi app đang chạy và hàng đợi rỗng: exit 0, app/worker thoát, ổ Windows bị tháo. WinFsp và hash các file cấu hình cá nhân/fixture kiểm tra vẫn nguyên.
- Đã cài lại EasyMount 0.3.5 sau kiểm thử. Không sử dụng ghi/xóa cloud cá nhân để thử uninstall.

Kết quả máy đọc được: `uninstall-integration-results.json`. Các log MSI chi tiết giữ riêng trong workspace thử, không đưa dữ liệu tài khoản vào repo.
