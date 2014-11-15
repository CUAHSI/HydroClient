var map;
var clusteredMarkersArray = [];


$(document).ready(function () {

    
    var myCenter = new google.maps.LatLng(41.6, -101.85);
    var marker = new google.maps.Marker({
        position: myCenter
    });
    initialize();

    function initialize() {
        var mapProp = {
            center: myCenter,
            zoom: 4,
            draggable: false,
            scrollwheel: false,
            mapTypeId: google.maps.MapTypeId.ROADMAP
        };

        map = new google.maps.Map(document.getElementById("map-canvas"), mapProp);
        //marker.setMap(map);

        //triger update of map on these events
        //google.maps.event.addListener(map, 'dblclick', function () {
        //    if ((infoWindow.getContent() == undefined) || (infoWindow.getContent() == "")) {
        //        updateMap(true)

        //    }

        //});
        //google.maps.event.addListener(map, 'dragend', function () {
        //    if ((infoWindow.getContent() == undefined) || (infoWindow.getContent() == "")) {
        //        updateMap(true)

        //    }

        //});
        //google.maps.event.addListener(map, 'zoom_changed', function () {
        //    if ((infoWindow.getContent() == undefined) || (infoWindow.getContent() == "")) {
        //        updateMap(true)

        //    }

        //});

        google.maps.event.addListener(marker, 'click', function () {

            infowindow.setContent(contentString);
            infowindow.open(map, marker);

        });
    };
    //$(document).on("click", ".alert", function (e) {
    //    bootbox.alert("Hello world!", function () {
    //        console.log("Alert Callback");
    //    });
    //});
    $('.input-daterange').datepicker()

    $("#Search").submit(function (e) {

        //prevent Default functionality
        e.preventDefault();
        e.stopImmediatePropagation();

        //get the action-url of the form
        var actionurl = '/home/SearchSubmit';
        var formdata = $("#Search").serializeArray()
        
        var bounds = map.getBounds();
        var ne = bounds.getNorthEast(); // LatLng of the north-east corner
        var sw = bounds.getSouthWest(); // LatLng of the south-west corder 
        var startDate = $("#startDate").val()
        var endDate = $("#endDate").val()
        var keywords = new Array();
        //variable.push  = $("#variable").val()
        var services = new Array();
        services.push(181);
        var selectedKeywords = $("input[name='keywords']:checked").map(function () {
            return $(this).val();
        }).get();
        if (selectedKeywords.length == 0)
        {
            //keywords.push("All");
        }
        else
        {
            keywords.push(keywordsValues);
        }

        var xMin = Math.min(ne.lng(), sw.lng())
        var xMax = Math.max(ne.lng(), sw.lng())
        var yMin = Math.min(ne.lat(), sw.lat())
        var yMax = Math.max(ne.lat(), sw.lat())
        var zoomLevel = map.getZoom();
        //formdata.push({ name: "bounds", value: xMin + ',' + xMax + ',' + yMin + ',' + yMax });
        //Extent
        formdata.push({ name: "xMin", value: xMin});
        formdata.push({ name: "xMax", value: xMax});
        formdata.push({ name: "yMin", value: yMin});
        formdata.push({ name: "yMax", value: yMax });
        formdata.push({ name: "zoomLevel", value: zoomLevel})
        //Date range
        formdata.push({ name: "startDate", value: startDate });
        formdata.push({ name: "endDate", value: endDate });
        //Keywords
        formdata.push({ name: "keywords", value: keywords });
        //Services
        formdata.push({ name: "services", value: services  });
            //do your own request an handle the results
        $.ajax({
            url: actionurl,
            type: 'post',
            dataType: 'json',
            data: formdata,
            success: function(data) {
                processMarkers(data)
            },
            error:serviceFailed
        });

    });

});

/*Menu-toggle*/
$("#menu-toggle").click(function (e) {
    e.preventDefault();
    $("#wrapper").toggleClass("active");
});

/*Scroll Spy*/
//$('body').scrollspy({ target: '#spy', offset: 80 });

/*Smooth link animation*/
$('a[href*=#]:not([href=#])').click(function () {
    if (location.pathname.replace(/^\//, '') == this.pathname.replace(/^\//, '') || location.hostname == this.hostname) {

        var target = $(this.hash);
        target = target.length ? target : $('[name=' + this.hash.slice(1) + ']');
        if (target.length) {
            $('html,body').animate({
                scrollTop: target.offset().top
            }, 1000);
            return false;
        }
    }
});
function processMarkers(geoJson)
{
    //map.data.loadGeoJson('https://storage.googleapis.com/maps-devrel/google.json');

    var geojson = JSON.parse(geoJson);
    map.data.addGeoJson(geojson);
    //zoom(map);

}

//upddate map wit new clusters
function updateMap() {
        deleteClusteredMarkersOverlays()
    //$("#timeOfLastRefresh").html("Last refresh: " + getTimeStamp());

}
// Deletes all markers in the array by removing references to them.
function deleteClusteredMarkersOverlays() {
    clearOverlays();
    clusteredMarkersArray.length = 0;
}
// Removes the overlays from the map, but keeps them in the array.
function clearOverlays() {
    setAllMap(null);
}
// Sets the map on all markers in the array.
function setAllMap(map) {
    for (var i = 0; i < clusteredMarkersArray.length; i++) {
        clusteredMarkersArray[i].setMap(map);
    }
}
// Deletes all markers in the array by removing references to them
function deleteOverlays(arrayName) {
    if (arrayName) {
        for (i in arrayName) {
            //google.maps.event.clearInstanceListeners(arrayName[i]);
            arrayName[i].setMap(null);

        }
        arrayName.length = [];
    }
}function serviceFailed(result) 
{
    //hideLoadingImage();
    //setup initial map
    var Latlng = new google.maps.LatLng(0, 0);
    zoomlevel = 6
    //AddMainMap(zoomlevel, Latlng)
    if (retryAttempts <= 3) {
        updateMap(true);
        retryAttempts++;
    }
    else 
    {
        alert('Service call failed. Please refresh page: ' + result.status + '' + result.statusText);
    }
}