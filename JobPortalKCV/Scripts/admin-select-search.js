(function ($) {
    function enhanceNiceSelectSearch() {
        $(".nice-select").each(function () {
            var $nice = $(this);
            var $list = $nice.find(".list");

            if (!$list.length || $list.find(".nice-select-search").length) {
                return;
            }

            var $search = $('<li class="nice-select-search"><input type="text" autocomplete="off" spellcheck="false" placeholder="Search..." /></li>');
            $list.prepend($search);

            $search.on("click mousedown mouseup touchstart touchend", function (event) {
                event.stopPropagation();
            });

            $search.find("input").on("click keydown keyup input", function (event) {
                event.stopPropagation();

                var keyword = ($(this).val() || "").toLowerCase();
                var hasVisibleOption = false;

                $list.find(".option").each(function () {
                    var $option = $(this);
                    var isMatch = !keyword || $option.text().toLowerCase().indexOf(keyword) >= 0;
                    $option.toggle(isMatch);
                    hasVisibleOption = hasVisibleOption || isMatch;
                });

                $list.find(".nice-select-empty").remove();

                if (!hasVisibleOption) {
                    $list.append('<li class="nice-select-empty">No results</li>');
                }
            });

            $nice.on("click", function () {
                if ($nice.hasClass("open")) {
                    setTimeout(function () {
                        $search.find("input").val("").trigger("input").focus();
                    }, 0);
                }
            });
        });
    }

    $(function () {
        var $selects = $("select");

        if ($.fn.niceSelect && $selects.length) {
            $selects.each(function () {
                var $select = $(this);
                if (!$select.next(".nice-select").length) {
                    $select.niceSelect();
                }
            });
        }

        enhanceNiceSelectSearch();
    });
})(jQuery);
