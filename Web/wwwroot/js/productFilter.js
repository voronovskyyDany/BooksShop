var SITE_PATH = "https://localhost:7173/";

$("#isRecomendedFilter").change(function () {
    let categoryId = $("#categoryIdInput").val();

    $.ajax({
        url: SITE_PATH + 'customer/home/filter',
        type: 'GET',
        data: { categoryId: categoryId, isRecomended: this.checked },
        dataType: 'html',
        success: function (data) {
            $('body').html(data);
        },
    });
});