# Thành phần bên thứ ba

Microsoft .NET 10 — runtime được phân phối cùng app (self-contained). Bản giấy phép và thông báo bên thứ ba nguyên văn từ gói Microsoft .NET SDK 10.0.403 đã xác minh SHA512 nằm tại `dotnet-LICENSE.txt` và `dotnet-THIRD-PARTY-NOTICES.txt` trong thư mục `third-party`. Runtime đi kèm bản ứng viên này là 10.0.14; không cần tải runtime khi cài.

WinFsp - Windows File System Proxy, Copyright (C) Bill Zissimopoulos

Repository và giấy phép: https://github.com/winfsp/winfsp

Bộ cài chứa nguyên vẹn MSI WinFsp 2025 (2.1.25156) do dự án phát hành, có chữ ký NAVIMATICS LLC. WinFsp được cấp phép GPLv3 kèm ngoại lệ FLOSS; toàn văn tại `WinFsp-License.txt` trong thư mục `third-party` sau khi cài. Mã nguồn tương ứng tag v2.1, commit `ddca7bd5481857a65ba552f643b8776fd070836f`, có trong `winfsp-v2.1-source.zip` cùng thư mục, bao gồm các script/hướng dẫn build upstream. MSI không bị chỉnh sửa hoặc ký lại.

Rclone 1.75.2 — Copyright (C) Nick Craig-Wood và những người đóng góp. Giấy phép MIT tại `rclone-LICENSE.txt`. Repository: https://github.com/rclone/rclone

EasyMount gọi rclone qua tiến trình riêng. Việc đóng gói các thành phần không thay đổi giấy phép riêng của chúng. Giấy phép và mã nguồn WinFsp được cung cấp cùng bản binary; nguồn/tài liệu được đóng gói riêng, không cần tải khi cài ứng dụng.

Bản 0.2 dùng rclone `v1.75.2-easymount.1`, được build từ nguồn v1.75.2 với bản vá `engine/priority.go` và `engine/apply-overlay.ps1`. Đây là bản đã sửa đổi, không phải binary chính thức của rclone. Nguồn upstream, hash và bản vá có trong repo; giấy phép MIT upstream được giữ nguyên.
