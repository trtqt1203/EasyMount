# EasyMount — 0.7.8-beta.1

Bản Beta công khai, chưa phải Stable. Bộ cài EasyMount hiện chưa được ký số. App dùng .NET 10 và giao diện Windows hỗ trợ tiếng Anh/Việt, sáng/tối, workspace do người dùng tạo, tìm kiếm và lọc ổ đĩa. Website và API metadata nằm trong `web/`; file và token cloud được xử lý trên máy người dùng, không chuyển qua backend website. Trạng thái kiểm chứng nằm trong [docs/RELEASE-STATUS.md](docs/RELEASE-STATUS.md), điều kiện phát hành nằm trong [docs/RELEASE-PLAN.md](docs/RELEASE-PLAN.md).

Ứng dụng miễn phí cho Windows x64: gắn từng tài khoản cloud hoặc gộp nhiều tài khoản thành một ổ trong File Explorer. Mã ứng dụng được phát hành theo MIT.

## Code signing policy

Signing preparation and the current unsigned status are documented in the
[Code signing policy](docs/CODE-SIGNING-POLICY.md). SignPath Foundation approval
is pending application; no free signing certificate has been granted yet.

## Cài đặt

Mục tiêu hỗ trợ chính là Windows 11 x64. Khi có bản phát hành được chấp nhận, dùng Setup từ GitHub Releases được liên kết trên [website chính thức](https://easymount.pages.dev/). Hiện API chưa công bố bản tải xuống Stable. Bộ cài ứng viên chứa runtime, engine và WinFsp; nếu Windows yêu cầu khởi động lại để hoàn tất driver, thực hiện trước khi kết nối ổ.

Chỉ phát hành bản Setup. Thư mục payload do build tạo ra trong `work/` chỉ phục vụ đóng gói và kiểm thử, không phải bản portable để phân phối.

Ứng dụng cài tại `C:\Program Files\EasyMount`. Khi gỡ, WinFsp được giữ lại vì là thành phần dùng chung; cấu hình và cache cũng được giữ. Bộ Setup hiện chưa được ký bằng chứng thư phát hành.

## Thêm tài khoản cloud

1. Chọn **Tài khoản cloud**, hoặc bấm **Thêm ổ đĩa** khi chưa có tài khoản.
2. Chọn dịch vụ trong danh sách. Có thể để trống tên để lấy email sau đăng nhập, hoặc tự nhập tên. Ở màn hình hoàn tất, bạn có thể sửa tên hiển thị; mã kết nối bên trong được giữ riêng. Nếu nhà cung cấp không trả về email, hãy nhập tên bạn muốn.
3. Bấm **Đăng nhập trên trình duyệt** để xác thực tại trang của nhà cung cấp. Khi cần chọn loại tài khoản/ổ, trả lời ngay trong cửa sổ EasyMount. Phạm vi phát hành gồm Google Drive, Microsoft OneDrive và Dropbox.
4. Chờ EasyMount kiểm tra quyền truy cập, bấm **Hoàn tất**. Nếu bắt đầu từ Thêm ổ đĩa, form tạo ổ sẽ mở tiếp. Có thể thêm nhiều tài khoản cùng dịch vụ; ứng dụng không giới hạn số lượng.
5. Có thể **Hủy** khi đang xác thực. Tài khoản mới chưa hoàn tất sẽ được dọn nếu dịch vụ cấu hình phản hồi; các tài khoản cũ được giữ. Nếu ứng dụng bị buộc dừng, có thể còn cấu hình dở cần kiểm tra lại.

Google Drive cần OAuth client riêng để sử dụng ổn định: client dùng chung của rclone đang được ngừng trong năm 2026. Khi bản build chưa được cấu hình OAuth của EasyMount, giao diện sẽ hướng dẫn nhập Client ID và Client Secret. Nguồn chính thức: https://rclone.org/drive/#making-your-own-client-id . Không coi việc có giao diện đăng nhập là bằng chứng đã xác thực thành công bằng tài khoản thật.

Bản 0.3.7 đã có OAuth client Desktop riêng của EasyMount trong `oauth-clients.json`; không cần nhập Client ID/Secret khi thêm Google Drive. File này chỉ chứa định danh ứng dụng, không chứa token, mật khẩu hoặc cấu hình tài khoản người dùng. Đây là public client của ứng dụng Desktop; không dùng client secret đi kèm như mật khẩu bảo vệ một dịch vụ máy chủ.

Google OAuth đã được chuyển sang Production và hồ sơ đang được Google xét duyệt theo trạng thái người quản lý cung cấp. Đây chưa phải xác nhận ứng dụng đã được phê duyệt. Giới hạn và cảnh báo của Google đối với ứng dụng chưa xác minh vẫn có thể áp dụng. Xem `docs/OAUTH-CHECKPOINT-20261106.md`.

Tên remote khác nhau không bảo đảm đó là hai tài khoản thực khác nhau. Tránh cấu hình cùng một tài khoản/thư mục dưới nhiều tên rồi đưa vào cùng một ổ gộp vì dung lượng có thể bị đếm lặp.

## Tạo một ổ gộp

1. Chọn **Thêm ổ đĩa**, nhập tên hiển thị và ký tự ổ, ví dụ `R:`.
2. Bật **Gộp nhiều tài khoản thành một ổ đĩa**.
3. Chọn tài khoản và thư mục, nhập giới hạn nếu cần, bấm **Thêm nguồn vào ổ gộp**. Thêm ít nhất hai nguồn.
4. Dùng **Lên / Xuống** để sắp thứ tự ưu tiên. Nguồn ở trên được xét trước.
5. Chọn **Kiểm tra**, lưu, rồi kết nối. Mặc định đọc và ghi; có thể chọn Chỉ đọc nếu chỉ muốn xem và tải file về.

File mới được ghi vào nguồn đầu tiên còn đủ chỗ chứa toàn bộ file. Ví dụ nguồn 1 còn 2 GiB, nguồn 2 còn 20 GiB: file 3 GiB sẽ được ghi nguyên vào nguồn 2. File nhỏ hơn vẫn có thể sử dụng dung lượng còn lại ở nguồn 1. Không chia file giữa nhiều tài khoản, không di chuyển hay cân bằng lại file cũ khi đổi thứ tự.

Giới hạn **0** dùng dung lượng trống do dịch vụ báo. Nếu dịch vụ không có API báo dung lượng, đặt giới hạn GiB cho thư mục đã chọn. Giới hạn thủ công áp dụng cho việc đặt **file mới**, tính tổng file trong thư mục đó; kiểm tra này cần quét thư mục và có thể chậm trên kho lớn. Nếu có cả quota API và giới hạn thủ công, dùng mức còn trống thấp hơn. Sửa file cũ giữ nguyên nơi lưu; quota vật lý của nhà cung cấp vẫn áp dụng.

Khi không nguồn nào đủ chỗ, thao tác upload báo lỗi và cache VFS được giữ lại. Khi quota API bị trễ hoặc cloud phát sinh lỗi trong lúc upload, giữ ứng dụng chạy và xử lý thông báo; không xem việc Windows đã ghi xong vào cache là upload đã hoàn tất. Lỗi mạng, quyền truy cập và hạn mức API không được coi là hết dung lượng.

Các thư mục cùng tên được hiển thị chung. Nếu nhiều nguồn đã có file cùng đường dẫn, bản ở nguồn ưu tiên cao hơn được đọc; thao tác sửa/xóa theo chính sách union `epall` có thể tác động các bản trùng. Nên dùng thư mục riêng, rỗng khi tạo ổ gộp mới.

## Đổi tên và vận hành

- Ngắt kết nối, chọn **Sửa**, đổi tên rồi lưu và kết nối lại. Tên mới được dùng làm nhãn ổ trong Explorer. Đổi tên không đổi tài khoản, thứ tự hoặc khóa phục hồi cache.
- Có thể đổi thứ tự và nguồn khi ổ đã ngắt và không còn cache cần giữ. Bản 0.2 chặn đổi/xóa nguồn nếu thư mục cache vẫn có file để tránh chuyển nhầm dữ liệu chờ upload. Kết nối bằng cấu hình cũ để hoàn tất phục hồi; chỉ dọn cache sau khi xác minh dữ liệu đã có trên cloud.
- Trước khi ngắt ổ ghi, đóng các file đang mở và chờ upload hoàn tất. Không xóa cache có dữ liệu chưa tải lên.
- Bấm **X** hoặc **Thoát ứng dụng** để kiểm tra upload, ngắt ổ và thoát. Nếu còn file chờ hoặc lỗi upload, app giữ kết nối và báo lý do. Dùng nút thu nhỏ nếu muốn giữ ổ hoạt động.
- Tự kết nối chỉ áp dụng khi mở EasyMount, không tự đăng ký chạy cùng Windows.
- Đây là truy cập cloud theo nhu cầu, không phải một bản sao ngoại tuyến đầy đủ.

Cấu hình nằm ở `%LOCALAPPDATA%\EasyMount`: `drives.json`, `rclone.conf`, `pools`, `logs`, `cache`, `sessions`. `rclone.conf` có thể chứa token. Không đưa cấu hình cá nhân vào repo.

## Build và kiểm tra

Cần SDK .NET 10.0.403 theo `global.json`, Go 1.26 trở lên và WiX 5.0.2. CI ghim Go 1.27.3 và các GitHub Actions theo commit. Engine là rclone v1.75.2 với overlay EasyMount .3 trong `engine/`; bản upstream không có bộ chọn ghi này. CI nguồn không thay thế kiểm thử cloud thật, Windows sạch hoặc chữ ký phát hành.

```powershell
.\build-setup.ps1 -DotnetPath 'C:\path\dotnet.exe' -GoPath 'C:\path\go.exe'
# Hoặc build payload riêng rồi đóng gói:
.\build.ps1 -DotnetPath 'C:\path\dotnet.exe' -GoPath 'C:\path\go.exe'
.\build-setup.ps1 -SkipAppBuild
```

Script tải mã nguồn rclone với hash đã ghim, áp bản vá và build engine có Windows mount (`cmount`). WinFsp MSI được kiểm tra hash và chữ ký chính thức trước khi đóng gói. Không script nào tự cài app hoặc driver lên máy build.

```powershell
dotnet run --project .\tests\EasyMount.Checks.csproj -- C:\path\work\easymount-build\app\tools\rclone.exe
python .\tests\check-pool.py C:\path\work\easymount-build\app\tools\rclone.exe
```

Xem `docs/VALIDATION.md` về kiểm chứng và phần chưa được thử. Tài liệu upstream: [rclone union](https://rclone.org/union/), [rclone mount](https://rclone.org/commands/rclone_mount/), [Google Drive](https://rclone.org/drive/), [OneDrive](https://rclone.org/onedrive/).

## Repo và website

- Mã nguồn: https://github.com/trtqt1203/EasyMount
- Trang chủ: https://easymount.pages.dev/
- Chính sách riêng tư: xem liên kết Privacy trên trang chủ. Tên miền và hồ sơ Google vẫn cần xác minh trước phát hành rộng.

Build payload/Setup yêu cầu `oauth-clients.json` có định danh Google Desktop client. Sao chép cấu trúc từ `oauth-clients.example.json`, điền client của bạn, hoặc truyền `-OAuthClientsPath` tới file riêng. Cấu hình rỗng bị từ chối; không còn tự dùng ví dụ rỗng để đóng gói. Không commit file đó, token, rclone.conf, drives.json hoặc thông tin tài khoản cá nhân. Tài khoản người dùng vẫn phải được Google cho phép và tự cấp quyền.

Kiểm thử cloud thật dùng `tests/check-cloud.py --engine PATH --config PATH --remote ACCOUNT: --report REPORT.json` (lặp `--remote` cho từng tài khoản). Script tạo dữ liệu thử trong thư mục ngẫu nhiên mới, kiểm tra upload/download SHA256, đổi tên, sửa và xóa đúng dữ liệu thử. Nó không kiểm thử đăng nhập tương tác, mount VFS hoặc cơ chế pool; các phần này cần bằng chứng riêng. API test `--api-test` dùng website công khai mặc định; đặt `EASYMOUNT_TEST_API_URL` để dùng server kiểm thử khác.

## Khôi phục phiên mount (0.3.7)

Khi mở lại, EasyMount nhận lại phiên rclone còn sống bằng PID, thời gian tạo tiến trình, đường dẫn executable, cấu hình nguồn và xác thực loopback. Mật khẩu điều khiển phiên được Windows DPAPI bảo vệ theo người dùng; không đặt trên dòng lệnh của worker mới. Không tự giết tiến trình lạ hoặc bỏ qua hàng đợi upload.

Sau khi gửi lệnh thoát, app chờ tiến trình dừng và Windows gỡ ổ trước khi báo ngắt xong. Nếu app bị End Task/mất điện, cache và thông tin phiên vẫn được giữ; quá trình khôi phục cần đúng tài khoản Windows và thư mục dữ liệu ban đầu. Phiên cũ trước 0.3.7 không tự có thông tin khôi phục; cần xác minh trước khi nhập lại.

Ổ ghi Google Drive bị chặn nếu cấu hình chỉ có scope drive.readonly. Muốn ghi cần đăng nhập/cấp quyền ghi đúng tài khoản, không chỉ bỏ chọn Chỉ đọc ở cấu hình ổ.


## Cập nhật 0.3.7

- Gắn ổ Google Drive với tên `ggdrive`, OneDrive với `onedrive`, ổ gộp với `easymount-pool`. Ngắt ổ dọn ánh xạ và thông báo Explorer.
- Danh sách ổ gộp dùng tên tài khoản, số ưu tiên và giới hạn dễ đọc. Chọn một dòng để sửa thư mục/giới hạn rồi Thêm / cập nhật. Lưu sẽ kiểm tra nguồn trước.
- Sau một lần kết nối và ngắt sạch, có thể đổi nguồn mà không bị cache tải về cũ chặn; cache cũ được giữ trong `cache-archive`. Cache chưa xác minh còn được bảo vệ.
- Sửa bước báo lỗi không có câu hỏi trong wizard OneDrive/các backend; lỗi nhà cung cấp được hiển thị để chọn lại thay vì kẹt Tiếp tục.
- Hướng dẫn độc lập, trạng thái EasyMount đã sẵn sàng; giới thiệu tác giả TRavisTRTQT và phiên bản đúng.


## Cập nhật 0.3.8

Ổ gộp mới mặc định tên **Drive chung**. Người dùng có thể nhập tên khác và đổi tên trong **Sửa → Tên ổ đĩa trong Explorer** sau khi ngắt kết nối. Tên tiếng Việt dài trên ổ mạng không còn bị cắt ở 32 byte.


## Cập nhật 0.3.9

Tên ổ gộp trong Explorer dùng hậu tố ngắn **(\\pool)**, để tên mặc định Drive chung và dòng dung lượng cùng hiển thị ở chế độ Tiles. Tên ổ do người dùng đặt vẫn được giữ.
