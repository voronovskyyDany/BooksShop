var SITE_PATH = "https://localhost:7173/";

$("#filterButton").click(function () {
    let categoryId = $("#categoryIdInput").val();

    let isRecomended = $('#isRecomendedFilter').is(":checked");
    let max = $('#two').val();
    let min = $('#one').val();
    let author = $("#authorFilter").val();

    $.ajax({
        url: SITE_PATH + 'customer/home/filter',
        type: 'GET',
        data: {
            categoryId: categoryId,
            isRecomended: isRecomended,
            author: author,
            max: max,
            min: min,
            
        },
        dataType: 'html',
        success: function (data) {
            $('body').html(data);
        },
    });
});