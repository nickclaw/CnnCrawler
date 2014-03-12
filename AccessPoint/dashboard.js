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

    var command = "start";
    $("#toggle").click(function () {
        sendCommand(command);
        command = command === "start" ? "stop" : "start";
    });

    window.setInterval(function (data) {
        sendRequest("QueueSize", null, function (data) {
            $("#queueSize .value").text(data.getElementsByTagName('int')[0].textContent);
        });

        sendRequest("CrawledSize", null, function (data) {
            $("#crawledSize .value").text(data.getElementsByTagName('int')[0].textContent);
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
        //
    }, 5000);
});