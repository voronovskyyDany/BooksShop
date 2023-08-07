$("#button-search").click(function () {
    var text = $('#input-search').val();

    if (text != '') {
        $.ajax({
            url: 'customer/home/search',
            type: 'GET',
            data: { text: text },
            dataType: 'html',
            success: function (data) {
                $('body').html(data);
            },
        });
    }
});