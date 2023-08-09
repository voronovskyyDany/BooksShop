var SITE_PATH = "https://localhost:7173/";
$(function () {
    let productId = $("#productIdInput").val();
    let favorites = [];
    $.ajax({
        url: SITE_PATH + 'customer/favorite/get',
        type: 'GET',
        dataType: 'html',
        success: function (data) {
            favorites = JSON.parse(data);

            if (favorites == null) {
                $("#favoriteButton").html('Add to Favorite');
                $("#favoriteButton").click(addToFavorite);
                return;
            }

            setNewButtonState(favorites, productId);
        },
    });
})
function addToFavorite() {
    let productId = $("#productIdInput").val();

    $.ajax({
        url: SITE_PATH + 'customer/favorite/add',
        type: 'POST',
        data: {
            productId: productId,
        },
        dataType: 'html',
        success: function (data) {
            let favorites = JSON.parse(data);
            setNewButtonState(favorites, productId);
        },
    });
}
function removeFromFavorite() {
    let productId = $("#productIdInput").val();

    $.ajax({
        url: SITE_PATH + 'customer/favorite/remove',
        type: 'DELETE',
        data: {
            productId: productId,
        },
        dataType: 'html',
        success: function (data) {
            let favorites = JSON.parse(data);
            setNewButtonState(favorites, productId);
        },
    });
}
function setNewButtonState(favorites, productId) {
    if (favorites.find((element) => element == productId) == null) {
        $("#favoriteButton").html('Add to Favorite');
        $("#favoriteButton").click(addToFavorite);
    }
    else {
        $("#favoriteButton").html('Remove from Favorite');
        $("#favoriteButton").click(removeFromFavorite);
    }
}