var map;
var clusteredMarkersArray = [];
var clusterMarkerPath = "/Content/Images/Markers/ClusterIcons/";
var markerPath = "/Content/Images/Markers/Services/";
var infoWindow;
var myDataTable;

$(document).ready(function () {

    var myCenter = new google.maps.LatLng(41.6, -101.85);
    var marker = new google.maps.Marker({
        position: myCenter
    });
    initialize();

    function initialize() {

        infoWindow = new google.maps.InfoWindow();

        var mapProp = {
            center: myCenter,
            zoom: 11,
            draggable: true,
            scrollwheel: true,
            mapTypeId: google.maps.MapTypeId.ROADMAP,
            mapTypeControl: true,
            scaleControl: true,
            mapTypeControlOptions: {
                style: google.maps.MapTypeControlStyle.HORIZONTAL_BAR,
                position: google.maps.ControlPosition.TOP_LEFT,
                mapTypeIds: [google.maps.MapTypeId.ROADMAP, google.maps.MapTypeId.SATELLITE, google.maps.MapTypeId.HYBRID, google.maps.MapTypeId.TERRAIN, 'topo']
            },
        };
        $("#map-canvas").height(getMapHeight()) //setMapHeight
        $("#map-canvas").width(getMapWidth()) //setMapWidth
        map = new google.maps.Map(document.getElementById("map-canvas"), mapProp);
        //marker.setMap(map);

        //triger update of map on these events
        google.maps.event.addListener(map, 'dblclick', function () {
            if ((infoWindow.getContent() == undefined) || (infoWindow.getContent() == "")) {
                updateMap(false)

            }

        });
        google.maps.event.addListener(map, 'dragend', function () {
            if ((infoWindow.getContent() == undefined) || (infoWindow.getContent() == "")) {
                updateMap(false)

            }

        });
        google.maps.event.addListener(map, 'zoom_changed', function () {
            if ((infoWindow.getContent() == undefined) || (infoWindow.getContent() == "")) {
                updateMap(false)

            }

        });

        google.maps.event.addListener(marker, 'click', function () {

            infowindow.setContent(contentString);
            infowindow.open(map, marker);

        });
        google.maps.event.addDomListener(window, "resize", function () {
            $("#map-canvas").height(getMapHeight()) //setMapHeight
            $("#map-canvas").width(getMapWidth()) //setMapWidth
            google.maps.event.trigger(map, "resize");
           
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

        updateMap(true)

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

function getMapHeight()
{
    var mapHeight = $(window).height() - $("#page-header").height() + "px";
    return mapHeight;
}
function getMapWidth() {
    var mapWidth = $(window).width() + "px";
    return mapWidth;
}

function processMarkers(geoJson)
{
    //map.data.loadGeoJson('https://storage.googleapis.com/maps-devrel/google.json');

    var json = JSON.parse(geoJson);
    //map.data.addGeoJson(geojson);
    //zoom(map);
    if (json != null) {
        // set title
        //$("#headerlbl").html(programname);

        //set legend html
       // var s = json.properties.legend;

        //if (s.length > 0) {
        //    $("#openlegend").html(s);
        //}

        //features = json.features;
        if (typeof json.features != "undefined") {
            for (var i = 0, len = json.features.length; i < len; ++i) {
                if (json.features[i].geometry.type == "Point") {

                    //get values
                    var clusterid = parseFloat(json.features[i].properties.clusterid);
                    var id = json.features[i].id;
                    var lat = parseFloat(json.features[i].geometry.coordinates[1]);
                    var lng = parseFloat(json.features[i].geometry.coordinates[0]);
                    var point = new google.maps.LatLng(lat, lng);
                    var count = 1;
                    if (typeof json.features[i].properties.count != "undefined") { count = parseFloat(json.features[i].properties.count) };
                    var icontype = json.features[i].properties.icontype;
                    //create marker
                    ///if (document.location.search.indexOf('ThemeName') != -1) {
                    marker = updateClusteredMarker(map, point, count, icontype, id, clusterid, "");
                    clusteredMarkersArray.push(marker);
                    
                    //}
                    //else {

                    //    marker = updateClusteredMarkerForTheme(point, clusteredMarkersIndex, count, icontype, id, clusterid, "");
                    //    clusteredMarkersArray.push(marker);
                    //}
                    //clusteredMarkersIndex++;

                }
                else if (json.features[i].geometry.type == "Polygon") {


                    var id = json.features[i].id
                    var guid = json.features[i].geometry.properties.guid;
                    var sector = json.features[i].geometry.properties.sector;
                    var assessment = json.features[i].geometry.properties.assessment;
                    var dateassessed = json.features[i].geometry.properties.dateassessed;
                    var strokeColor = json.features[i].geometry.properties.strokeColor;
                    var strokeWeight = json.features[i].geometry.properties.strokeWeight;
                    var strokeOpacity = json.features[i].geometry.properties.strokeOpacity;
                    var fillColor = json.features[i].geometry.properties.fillColor;
                    var fillOpacity = json.features[i].geometry.properties.fillOpacity;
                    var label = ""; json.features[i].geometry.properties.sector;
                    //            label += "<br>" + polygons[k].assessment;
                    //            label += "<br>Assessed:" + polygons[k].dateassessed;
                    //            label += "<br>Posted:" + polygons[k].dateposted;

                    //            var vertices = answertext.split(" ");

                    var pts = new Array();

                    var points = json.features[i].geometry.coordinates;
                    for (l = 0; l < points.length - 1; l++) {
                        var p = points[l]
                        var pt = new google.maps.LatLng(p[0], p[1]);
                        pts.push(pt)


                    }
                    polygonMarker = createPolygon(pts, label, id, strokeColor, strokeWeight, strokeOpacity, fillColor, fillOpacity)


                    //polygonMarkerCenter = polygonMarkerbounds.getCenter()
                    //            //Extends bounds for corners
                    //            bounds.extend(polygonMarkerbounds.getSouthWest());
                    //            bounds.extend(polygonMarkerbounds.getNorthEast());

                    clusteredMarkersArray.push(polygonMarker);
                    polygonMarker.setMap(map);

                }
            };
        }
    }
}
function getFormData()
{
    var formdata = $("#Search").serializeArray()

    var bounds = map.getBounds();
    var ne = bounds.getNorthEast(); // LatLng of the north-east corner
    var sw = bounds.getSouthWest(); // LatLng of the south-west corder 
    var startDate = $("#startDate").val()
    var endDate = $("#endDate").val()
    var keywords = new Array();
    //variable.push  = $("#variable").val()
    var services = new Array();
    //services.push(181);
    var selectedKeywords = $("input[name='keywords']:checked").map(function () {
        return $(this).val();
    }).get();
    if (selectedKeywords.length == 0) {
        //keywords.push("All");
    }
    else {
        keywords.push(keywordsValues);
    }

    var xMin = Math.min(ne.lng(), sw.lng())
    var xMax = Math.max(ne.lng(), sw.lng())
    var yMin = Math.min(ne.lat(), sw.lat())
    var yMax = Math.max(ne.lat(), sw.lat())
    var zoomLevel = map.getZoom();
    //formdata.push({ name: "bounds", value: xMin + ',' + xMax + ',' + yMin + ',' + yMax });
    //Extent
    formdata.push({ name: "xMin", value: xMin });
    formdata.push({ name: "xMax", value: xMax });
    formdata.push({ name: "yMin", value: yMin });
    formdata.push({ name: "yMax", value: yMax });
    formdata.push({ name: "zoomLevel", value: zoomLevel })
    //Date range
    formdata.push({ name: "startDate", value: startDate });
    formdata.push({ name: "endDate", value: endDate });
    //Keywords
    formdata.push({ name: "keywords", value: keywords });
    //Services
    formdata.push({ name: "services", value: services });


    return formdata;
}
//upddate map wit new clusters
function updateMap(isNewRequest) {
    

    if (clusteredMarkersArray.length == 0 && isNewRequest == false) return;//only map navigation

    var formData = getFormData();

    //get the action-url of the form
    var actionurl = '/home/updateMarkers';
    //Clean up
    if (clusteredMarkersArray.length == 0 || isNewRequest == true) {

        formData.push({ name: "isNewRequest", value: true });
    }
    else {
   
        deleteClusteredMarkersOverlays()
    }
   //get Markers
    var actionurl = '/home/updateMarkers';
    $.ajax({
        url: actionurl,
        type: 'POST',
        dataType: 'json',
        timeout: 60000,
        //processData: false,
        data: formData,
        success: function (data) {
            processMarkers(data)
        },
        error: function (xmlhttprequest, textstatus, message) {
            serviceFailed(xmlhttprequest, textstatus, message)
        }
    });
    
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
}

function updateClusteredMarker(map, point, count, icontype, id, clusterid, label) {

    var icon_choice;
    var icon;
    var icons;


    icons = {
        0: { file: "blue-22.png", printImage: "blue-22.gif", width: 44, cssClass: "GblueLabel22", labelXoffset: 22, labelYoffset: 22 },
        1: { file: "blue-24.png", printImage: "blue-24.gif", width: 44, cssClass: "GblueLabel24", labelXoffset: 22, labelYoffset: 22 },
        2: { file: "blue-28.png", printImage: "blue-28.gif", width: 44, cssClass: "GblueLabel28", labelXoffset: 22, labelYoffset: 22 },
        3: { file: "blue-32.png", printImage: "blue-32.gif", width: 44, cssClass: "GblueLabel32", labelXoffset: 22, labelYoffset: 22 },
        4: { file: "blue-36.png", printImage: "blue-36.gif", width: 44, cssClass: "GblueLabel36", labelXoffset: 22, labelYoffset: 22 },
        5: { file: "blue-40.png", printImage: "blue-40.gif", width: 44, cssClass: "GblueLabel40", labelXoffset: 22, labelYoffset: 22 },
        6: { file: "blue-44.png", printImage: "blue-44.gif", width: 44, cssClass: "GblueLabel44", labelXoffset: 22, labelYoffset: 22 }
    }

    //var clusterMarkerPath = "./images/markers/Cluster/";

    if (count == 1) {

        var marker = new MarkerWithLabel({
            position: point,
            icon: new google.maps.MarkerImage(markerPath + icontype.split('.')[0] + '.png', new google.maps.Size(32, 32), null, null, new google.maps.Size(28, 28)),
            draggable: false,
            raiseOnDrag: true,
            map: map,
            anchorPoint: new google.maps.Point(14, 32),
            labelContent: "",
            //labelAnchor: new google.maps.Point(0, 0),
            //labelClass: "labels", // the CSS class for the label
            labelStyle: { opacity: 0.95 },
            tooltip: '',
            zIndex: 1500,
            flat: true,
            visible: true

        });


        //        icon = "./images/markers/assessments/" + icontype + ".png";
        //        size = new google.maps.Size(15, 15);
        //        icon.size = size;
        //        scaledSize = new google.maps.Size(15, 15);
        //        icon.scaledSize = scaledSize;
        //        var img = new google.maps.MarkerImage('./images/markers/assessments/' + icontype + '.png', new google.maps.Size(32, 32), null,null, new google.maps.Size(25, 25));
        //        var marker = clusteredMarkersArray[clusteredMarkersIndex];
        //        marker.setVisible(true);
        //        marker.setIcon(img);

        //marker.setIcon();
        //        marker.setPosition(point);
        //        marker.labelContent= "";

        //st Zindex
        //       marker.setZIndex(1499);
        //attach to map
        //       if (marker.getMap != null) marker.setMap(map)

        //        clusteredMarkersArray.push(marker);

        //        marker.tooltip = "Assessments";

        //        var tooltip = new Tooltip({ map: map }, marker);
        //        tooltip.bindTo("text", marker, "tooltip");
        //        google.maps.event.addListener(marker, 'mouseover', function () {

        //            tooltip.addTip();
        //            tooltip.getPos2(marker.getPosition());

        //        });

        google.maps.event.addListener(marker, 'click', function () {
            getTabsForMarker(marker)
            //infoWindow.setContent(c);
            //infoWindow.open(map, this);
        });
        //var infoBox = new InfoBox({ latlng: marker.getPosition(), map: map });

    }

    else {
        if (count < 5) {
            icon_choice = 0;
            icon_height = 28;
            icon_width = 28;
        }
        else if (count < 10) {
            icon_choice = 0;
            icon_height = 30;
            icon_width = 30;
        }
        else if (count < 25) {
            icon_choice = 1;
            icon_height = 32;
            icon_width = 32;
        }
        else if (count < 50) {
            icon_choice = 2;
            icon_height = 36;
            icon_width = 36;
        }
        else if (count < 100) {
            icon_choice = 3;
            icon_height = 40;
            icon_width = 40;
        }
        else if (count < 250) {
            icon_choice = 3;
            icon_height = 44;
            icon_width = 4;
        }
        else if (count < 500) {
            icon_choice = 4;
            icon_height = 48;
            icon_width = 48;
        }
        else if (count < 1000) {
            icon_choice = 5;
            icon_height = 52;
            icon_width = 52;
        }
        else if (count < 5000) {
            icon_choice = 6;
            icon_height = 56;
            icon_width = 56;
        }
        else if (count > 5000) {
            icon_choice = 6;
            icon_height = 60;
            icon_width = 60;
            //count=""
        }
        //icon = new google.maps.MarkerImage(clusterMarkerPath + "all_sprite.png", new google.maps.Size(44, 44), new google.maps.Point(icon_choice, 0), new google.maps.Point(22, 22));

        //icon = new google.maps.MarkerImage(clusterMarkerPath + icons[icon_choice].file, null, null, new google.maps.Point(22, 22));

        var marker = new MarkerWithLabel({
            position: point,
            icon: new google.maps.MarkerImage(clusterMarkerPath + "m1.png", new google.maps.Size(53, 52), null, new google.maps.Point(icon_width / 2, icon_width / 2), new google.maps.Size(icon_width, icon_width)),
            draggable: false,
            raiseOnDrag: true,
            map: map,
            //anchorPoint: new google.maps.Point(0, 0),
            labelContent: count,
            labelAnchor: new google.maps.Point(22, 30),
            labelClass: icons[icon_choice].cssClass, // the CSS class for the label
            labelStyle: { opacity: 0.95 },
            tooltip: '',
            zIndex: 1500,
            flat: true,
            visible: true

        });

        //   var marker = clusteredMarkersArray[clusteredMarkersIndex]
        //    marker.setVisible(true);
        //    marker.setIcon(icon);
        //    marker.setPosition(point);

        //    marker.labelContent = count;
        //    marker.labelClass = icons[icon_choice].cssClass;
        //    marker.labelAnchor = new google.maps.Point(icons[icon_choice].labelXoffset, icons[icon_choice].labelYoffset);
        //    //st Zindex
        //    marker.setZIndex(1500); 
        //    //attach to map
        //    if (marker.getMap != null) marker.setMap(map)

        //    clusteredMarkersArray.push(marker);
        //google.maps.event.clearInstanceListeners(marker);






        google.maps.event.addListener(marker, 'click', function () {
            //var c = getDetailforCluster(id, clusterid)
            //infoWindow.setContent(c);
            //infoWindow.open(map, this);
            //$('#example').DataTable();
            setUpDatatables(clusterid);
            $('#testModal').modal('show')
            //var details = getDetailsForMarker(clusterid)
            //createInfoWindowContent()
           
        });
        //        google.maps.event.addListener(marker, "click", function (e) {
        //            var infoBox = new InfoBox({ latlng: marker.getPosition(), map: map });
        //        });

        //markersLabelArray.push(label);


    }
    return marker
}
function getDetailsForMarker(clusterid)
{
    var actionUrl = "/home/getDetailsForMarker/" + clusterid

   // $('body').modalmanager('loading');
     
    
    $.ajax({
        url: actionUrl,
        type: 'POST',
        dataType: 'json',
        timeout: 60000,
        processData: false,
        //data: formData,
        success: function (data) {
           
        },
        error: function (xmlhttprequest, textstatus, message) {
            serviceFailed(xmlhttprequest, textstatus, message)
        }
    });

      


}

function createInfoWindowContent()
{
    $('#example').DataTable();
   
    var $modal = $('#ajax-modal');
    //$modal.show();
    //$modal.on('click', '.update', function () {
    //    $modal.modal('loading');
    //    setTimeout(function () {
    //        $modal
    //        .modal('loading')
    //        .find('.modal-body')
    //          .prepend('<div class="alert alert-info fade in">' +
    //            'Updated!<button type="button" class="close" data-dismiss="alert">&times;</button>' +
    //          '</div>');
    //    }, 1000);
    //});
}

function setUpDatatables(clusterid)
{
    
//    var dataSet = [
//    ['Trident','Internet Explorer 4.0','Win 95+','4','X'],
//    ['Trident','Internet Explorer 5.0','Win 95+','5','C'],
 
    //];
    $.fn.DataTable.isDataTable("#example")
    {
        $('#example').DataTable().clear().destroy();
    }
    

    //var dataSet = getDetailsForMarker(clusterid)
    var actionUrl = "/home/getDetailsForMarker/" + clusterid
   // $('#example').DataTable().clear()
   // $('#demo').html( '<table cellpadding="0" cellspacing="0" border="0" class="display" id="example"></table>' );
 
    myDataTable = $('#example').dataTable( {
        "ajax": actionUrl,
        "columns": [
            { "data": "ServCode" },
            { "data": "ServURL" },
            { "data": "SiteCode" },
            { "data": "VariableCode"},
            { "data": "VariableName"},
            { "data": "BeginDate" },
            { "data": "EndDate" },
            { "data": "ValueCount" },
            { "data": "SiteName" },
            { "data": "Latitude" },
            { "data": "Longitude" },
            { "data": "DataType" },
            { "data": "ValueType" },
            { "data": "SampleMedium" },
            { "data": "TimeUnit" },
            { "data": "GeneralCategory" },
            { "data": "TimeSupport" },
            { "data": "ConceptKeyword" },
            { "data": "IsRegular" },
            { "data": "VariableUnits" },
            { "data": "Citation" }
        ],
        "scrollX": true,
        initComplete: function () {
            var api = this.api();
 
            api.columns().indexes().flatten().each( function ( i ) {
                var column = api.column( i );
                var select = $('<select><option value=""></option></select>')
                    .appendTo( $(column.footer()).empty() )
                    .on( 'change', function () {
                        var val = $.fn.dataTable.util.escapeRegex(
                            $(this).val()
                        );
 
                        column
                            .search( val ? '^'+val+'$' : '', true, false )
                            .draw();
                    } );
 
                column.data().unique().sort().each( function ( d, j ) {
                    select.append( '<option value="'+d+'">'+d+'</option>' )
                } );
            } );
        }
    


        //"retrieve": true
    });

    $('#example tbody').on('click', 'tr', function () {
        $(this).toggleClass('selected');
    });

    $('#button').click(function () {
        alert(table.rows('.selected').data().length + ' row(s) selected');
    });

} 
function serviceFailed(xmlhttprequest, textstatus, message)
{
    //hideLoadingImage();
    
    //AddMainMap(zoomlevel, Latlng)
    //if (retryAttempts <= 3) {
    //    updateMap(true);
    //    retryAttempts++;
    //}
    //else 
    //{
        alert('Service call failed. Please refresh page: ' + xmlhttprequest.status + '' + xmlhttprequest.statusText);
    //}
}