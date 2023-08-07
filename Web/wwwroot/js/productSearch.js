var SITE_PATH = "https://localhost:7173/";

$("#button-search").click(function () {
    let text = $('#input-search').val();
    let categoryId = $("#categoryIdInput").val();

    $.ajax({
        url: SITE_PATH + 'customer/home/search',
        type: 'GET',
        data: { text: text, categoryId: categoryId },
        dataType: 'html',
        success: function (data) {
            $('body').html(data);
        },
    });
});