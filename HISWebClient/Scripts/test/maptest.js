var slider;
var map;
$(function () {
  
    slider = $("#slider").slideReveal({
         width: 250,
         push: false,
        position: "right",
        top: 50,
        speed: 300,
        trigger: $("#trigger"),
        // autoEscape: false,
        show: function (obj) {
             $("#map-canvas").width(($(window).width() - $('#slider').width())) //setMapWidth
            if (typeof(map) !="undefined") google.maps.event.trigger(map, "resize");
        },
        shown: function (obj) {
            //$("#map-canvas").position({ left:2* $('#slider').width() })
            //$("#map-canvas").position({ right: $('#slider').width() })
           // $("#map-canvas").width(($(window).width() - $('#slider').width())) //setMapWidth
           // $("#map-canvas").position({ left:0 })
           // google.maps.event.trigger(map, "resize");
            console.log(obj);
        },
        hide: function (obj) {
            $("#map-canvas").position({ left: -$('#slider').width() })
            $("#map-canvas").width(($(window).width())) //setMapWidth

            google.maps.event.trigger(map, "resize");
        },
        hidden: function (obj) {
            console.log(obj);
        }
    });
  slider.slideReveal("show")
    google.maps.event.addDomListener(window, 'load', initialize);
    
});

function initialize() {
    var mapOptions = {
        zoom: 4,
        center: new google.maps.LatLng(-33, 151),
        mapTypeControl: true,
        mapTypeControlOptions: {
            style: google.maps.MapTypeControlStyle.DEFAULT,
            mapTypeIds: [
                google.maps.MapTypeId.ROADMAP,
                google.maps.MapTypeId.TERRAIN,
                google.maps.MapTypeId.SATELLITE
            ]
        },
        zoomControl: true,
        zoomControlOptions: {
            style: google.maps.ZoomControlStyle.SMALL
        }
    };
   
    $("#map-canvas").height(getMapHeight()) //setMapHeight
    $("#map-canvas").width(getMapWidth()) //setMapWidth

    map = new google.maps.Map(document.getElementById('map-canvas'),
                                    mapOptions);

          
}



function getMapHeight() {

    toolbarHeight = $(document).height() - $(window).height();
    var mapHeight = $(document).height() - toolbarHeight + "px";
    return mapHeight;
}

function getMapWidth(panelVisible) {
    var panelwidth = 0;

     panelwidth = $('#slider').width();

    //var mapWidth = $(window).width() - panelwidth + "px";
    var mapWidth = $(window).width() - panelwidth + "px";
    return mapWidth;
}
