# Tích hợp Reverse Engineer Anything

## Nguồn được kiểm tra

Liên kết yêu cầu: https://github.com/AIshihzong/awesome-reverse-engineer-anything

Repo này là danh sách tài nguyên và trường hợp thực tế, giấy phép CC0-1.0; không có `SKILL.md`. Phần **Official resources** của README liên kết tới skill chính thức:

https://github.com/morluto/rea/tree/main/.agents/skills/reverse-engineer-anything

Đã cài skill bằng helper `install-skill-from-github.py` của Codex từ repo `morluto/rea`, đường dẫn `.agents/skills/reverse-engineer-anything`, ghim commit:

`c0c3902207d6b1a33709e42b80c7e570da7db964`

Skill version `55`, giấy phép MIT. Có 8 tệp được cài tại:

`C:\Users\Administrator\.codex\skills\reverse-engineer-anything`

SHA-256 của `SKILL.md` đã cài khớp tệp upstream tại commit trên:

`5E074E4B9B01358291ABA1D2119B3766B360BE5B64483CE1D3FE5F2649B363C1`

Thông tin máy đọc được lưu trong `rea-integration.json`; giấy phép trong `REA-LICENSE.txt`. Không sao chép toàn bộ repo danh sách vào sản phẩm.

## Phạm vi tích hợp

Đã cài **skill hướng dẫn cho agent phát triển**, có thể sử dụng từ lượt hội thoại tiếp theo. Việc này không tự đăng ký REA MCP, không cài Ghidra/IDA hay engine phân tích và không thêm công cụ REA vào ứng dụng EasyMount.

Skill yêu cầu dùng REA khi kết luận phụ thuộc vào binary/package đã phát hành hoặc quan sát runtime cần thiết. Với phân tích mã nguồn thông thường, dùng công cụ repo và công cụ lập trình trực tiếp. Khi xây lại tính năng, phân biệt rõ quan sát, suy luận và phần chưa biết; kiểm chứng kết quả bằng hành vi thực tế.

## Áp dụng trong EasyMount

| Nội dung | Bằng chứng / trạng thái |
| --- | --- |
| Xác định đúng skill từ liên kết yêu cầu | README danh sách dẫn tới `morluto/rea`; cài thành công và đối chiếu hash |
| Nền tảng mount ổ đĩa | Chọn rclone + WinFsp theo tài liệu công khai; đây là quyết định triển khai |
| Tái dựng tính năng tương tự RaiDrive | Dựa trên mục tiêu sử dụng được yêu cầu và tài liệu công khai của các thành phần |
| Phân tích binary RaiDrive | Chưa thực hiện; không có artifact RaiDrive được cung cấp hoặc phân tích trong công việc này |
| REA MCP / engine phân tích | Chưa cài và chưa đăng ký |
| Hoạt động cloud thực tế | Cần WinFsp và tài khoản được người dùng cấp quyền; xem kết quả kiểm tra riêng |

Không gán Evidence ID của REA cho các bước chỉ dùng shell, mã nguồn hoặc tài liệu công khai. Khi có yêu cầu điều tra binary cụ thể, cần ghi nhận đường dẫn, phiên bản, SHA-256 và câu hỏi hành vi trước khi chọn công cụ; chỉ khẳng định những điều bằng chứng thu được chứng minh.
