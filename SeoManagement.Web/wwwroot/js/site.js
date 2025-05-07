// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {
    $('#urlTable').DataTable({
        "language": {
            "lengthMenu": "Hiển thị _MENU_ bản ghi mỗi trang",
            "zeroRecords": "Không tìm thấy bản ghi nào",
            "info": "Hiển thị trang _PAGE_ của _PAGES_",
            "infoEmpty": "Không có bản ghi nào",
            "infoFiltered": "(lọc từ _MAX_ bản ghi)",
            "search": "Tìm kiếm:",
            "paginate": {
                "first": "Đầu",
                "last": "Cuối",
                "next": "Tiếp",
                "previous": "Trước"
            },
        },
        "pageLength": 10,
        "lengthMenu": [5, 10, 25, 50],
        "order": [[3, "desc"]]
    });
});