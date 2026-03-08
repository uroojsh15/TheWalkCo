$(document).ready(function () {

    function doSearch() {

        var $input = $("#searchInput");
        var $results = $("#searchResults");
        var $page = $("#pageContent");

        var term = $input.val()?.trim(); // prevent crash

        if (!term) {
            $results.hide().empty();
            $page.show();
            return;
        }

        if ($results.length === 0 || $page.length === 0) return;

        $page.hide();
        $results
            .show()
            .html("<p class='text-muted'>Searching...</p>");

        $.get("/Product/Search", { term: term })
            .done(function (html) {
                $results.html(html);
            })
            .fail(function () {
                $results.html("<p class='text-danger'>Search failed</p>");
            });
    }

    // IMPORTANT FIX
    $("#searchButton").off("click").on("click", doSearch);

    $("#searchInput").off("keypress").on("keypress", function (e) {
        if (e.which === 13) {
            e.preventDefault();
            doSearch();
        }
    });

});
