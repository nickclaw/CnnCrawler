$(function () {

    function sendRequest(service, data, callback) {
        $.ajax({
            url: '/Service.asmx/' + service,
            type: 'POST',
            dataType: 'xml',
            data: data,
            success: function(data, status, xhr) {
                callback && callback(data);
            },
            error: function(xhr, status, error) {
                
            }
        })
    }

    function sendCommand(command) {
        sendRequest("Command", {'command': command });
    }

    function update() {
        sendRequest("QueueSize", null, function (data) {
            $("#queueSize .value").text(data.getElementsByTagName('int')[0].textContent);
        });

        sendRequest("CrawledSize", null, function (data) {
            $("#crawledSize .value").text(data.getElementsByTagName('int')[0].textContent);
        });

        sendRequest("ErrorSize", null, function (data) {
            $("#errorCount .value").text(data.getElementsByTagName('int')[0].textContent);
        });

        sendRequest("LastTen", null, function (data) {
            $("#lastten ul")
                .empty()
                .append($.map(data.getElementsByTagName('string'), function (value, index) {
                    return $('<li>', {
                        text: value.textContent
                    })
                }));
        });

        sendRequest("LastTenErrors", null, function (data) {
            $("#lasterrors ul")
                .empty()
                .append($.map(data.getElementsByTagName('string'), function (value, index) {
                    return $('<li>', {
                        text: value.textContent
                    })
                }));
        });

        sendRequest("GetRam", null, function (data) {
            $("#ram .value").text(data.getElementsByTagName("long")[0].textContent + 'mb');
        });

        sendRequest("IsRunning", null, function (data) {
            $("#running .value").text(data.getElementsByTagName("string")[0].textContent);
        });
    }

    var command = "start";
    $("#toggle").click(function () {
        sendCommand(command);
        command = command === "start" ? "stop" : "start";
        $(this).text(command);
    });

    update();
    window.setInterval(function () {
        update();
    }, 5000);
});