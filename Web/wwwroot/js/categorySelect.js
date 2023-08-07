$(".category-select").click(function () {
    var categoryId = $(this).attr("id");

    $.ajax({
        url: 'customer/home/index',
        type: 'GET',
        data: { id: categoryId },
        dataType: 'html',
        success: function (data) {
            $('body').html(data);
        },
    });
});