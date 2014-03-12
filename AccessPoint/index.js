$(function() {
    // searches for the given term and fills the results
    var search = function (term, callback) {
        if (term.trim() === "") {
            $("#results").empty().append("<div class='result'><h2>Search anything.</h2></div>").show();
            callback(null, []);
            return;
        } else {
            term = term.trim();
        }

        $.getJSON("ec2-54-186-115-202.us-west-2.compute.amazonaws.com?callback=test&name=" + term, function (data) {
            if (data.stats) {
                $("#playerContainer")
                    .empty()
                    .append(Object.keys(data.stats).map(function(key) {
                        return $("<div><span class='stat'>"+key+"</span>" + data.stats[key]);
                    });
            } else {
                $("#playerContainer").empty();
            }
        });

        $.ajax({
            url: '/Service.asmx/SearchUrl',
            type: 'POST',
            dataType: 'xml',
            data: {'word': term},
            success: function(data, status, xhr) {
                var urls = data.getElementsByTagName('string');
                console.log(urls);
                if (urls.length === 0) {
                    $("#no-results").show().find("#searchTerm").text(term);
                    $("#results").hide();
                } else {
                    $("#results")
                        .empty()
                        .append($.map(urls, function(value, index) {
                            return $("<div>", {
                                class: 'result',
                                append: $("<a>", {
                                    href: value.textContent,
                                    text: value.textContent
                                })
                            });
                        }))
                        .show();
                    $("#no-results").hide();
                }

                callback(null, data);
            },
            error: function (xhr, status, error) {
                $("#no-results").show().find("#searchTerm").text(term);
                $("#results").hide();
                // show error message, empty results
                callback(error, null);
            }
        });
    }

    // looks up the autocomplete strings and shows them
    var autocomplete = function(string, callback) {
        $("#autocomplete")
            .show()
            .addClass("loading");

        $.ajax({
            url: '/path/to/service.xml',
            type: 'POST',
            dataType: 'xml',
            success: function(data, status, xhr) {
                $("#autoloading").removeClass("loading");
                // fill results
                callback(null, data);
            },
            error: function(xhr, status, error) {
                // show error message, empty results
                callback(error, null);
            }
        });
    }

    // whenever the something is typed in the searchbar
    var _t1 = null;
    var _t2 = null;
    $("#search").on('keyup', function(evt) {
        // show loading bar

        $('#resultsContainer').addClass('loading');

        // get autocomplete
        _t2 && clearTimeout(_t2);
        _t2 = setTimeout(function() {
            _t2 = null;

            // autocomplete
            autocomplete(evt.target.value, function() {
                // do nothing?
            });

        }, 100);

        // 500 ms after typing is finished, autosearch
        _t1 && clearTimeout(_t1);
        _t1 = setTimeout(function() {
            _t1 = null;

            // search
            search(evt.target.value, function() {
                $('#resultsContainer').removeClass('loading');
            });

        }, 200);

        // if enter is pressed, immediately search and clear timeout
    }).on('keydown', function(evt) {
        if (evt.which === 13) {
            evt.preventDefault();
            clearTimeout(_t1);
            _t1 = null;

            search(evt.target.value, function() {
                $('#results').removeClass('loading');
            });
        }
    });

    $("#autocomplete").on('click', 'li', function(evt) {
        var e = $.Event('keydown');
        e.which = 13;
        $("#search")[0].value = evt.target.innerHTML;
        $("#search").trigger(e);
    });

    $(document.body).on('click', function() {
        $('#autocomplete').hide();
    })

});
