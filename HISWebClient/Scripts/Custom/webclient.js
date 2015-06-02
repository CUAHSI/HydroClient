var map;
var clusteredMarkersArray = [];
var areaRect;
var clusterMarkerPath = "/Content/Images/Markers/ClusterIcons/";
var markerPath = "/Content/Images/Markers/Services/";
var infoWindow;
var myTimeSeriesList;
var myTimeSeriesClusterDatatable;
var myServicesList;
var myServicesDatatable;
var mySelectedServices = [];
var mySelectedTimeSeries = [];
var sessionGuid;
var slider;
var sidepanelVisible = false;

$(document).ready(function () {

    $("#pageloaddiv").hide();
    initialize();
   
})

function initialize() {

    //var myCenter = new google.maps.LatLng(39, -92); //us
    //var myCenter = new google.maps.LatLng(42.3, -71);//boston
    var myCenter = new google.maps.LatLng(41.7, -111.9);//Salt Lake
    

    infoWindow = new google.maps.InfoWindow();

    //init list od datatables for modal 
    myServicesList = setupServices();

    var mapProp = {
        center: myCenter,
        zoom: 9,
        draggable: true,
        scrollwheel: true,
        mapTypeId: google.maps.MapTypeId.ROADMAP,
        mapTypeControl: true,
        scaleControl: true,
        overviewMapControl: true,
        zoomControl: true,
        panControl:false,        
        zoomControlOptions: {     
            style: google.maps.ZoomControlStyle.SMALL,
            position: google.maps.ControlPosition.RIGHT_BOTTOM
        },
        mapTypeControlOptions: {
            style: google.maps.MapTypeControlStyle.DEFAULT,
            position: google.maps.ControlPosition.LEFT_BOTTOM,
            mapTypeIds: [google.maps.MapTypeId.ROADMAP, google.maps.MapTypeId.HYBRID, google.maps.MapTypeId.TERRAIN]
        },
    };

    $("#map-canvas").height(getMapHeight()) //setMapHeight
    $("#map-canvas").width(getMapWidth()) //setMapWidth
    map = new google.maps.Map(document.getElementById("map-canvas"), mapProp);
    

    //UI
    
    addCustomMapControls();

    slider = addSlider();
 
    slider.slideReveal("show");
  
    addLocationSearch();
    
   
  


    //triger update of map on these events
    google.maps.event.addListener(map, 'dblclick', function () {
        if ((infoWindow.getContent() == undefined) || (infoWindow.getContent() == "")) {
            updateMap(false)
            //$("#MapAreaControl").html(getMapAreaSize());
        }
    });
    google.maps.event.addListener(map, 'dragend', function () {
        if ((infoWindow.getContent() == undefined) || (infoWindow.getContent() == "")) {
            updateMap(false)
            //$("#MapAreaControl").html(getMapAreaSize());
        }
    });
    
    google.maps.event.addListener(map, 'zoom_changed', function () {
        if ((infoWindow.getContent() == undefined) || (infoWindow.getContent() == "")) {
            updateMap(false)
            
            //$("#MapAreaControl").html(getMapAreaSize());
            
        }
    });
    //added to load size on startup
    google.maps.event.addListener(map, 'bounds_changed', function () {
        if ((infoWindow.getContent() == undefined) || (infoWindow.getContent() == "")) {
            updateMap(false)

            $("#MapAreaControl").html(getMapAreaSize());

        }
    });

    //google.maps.event.addListener(marker, 'click', function () {

    //    infowindow.setContent(contentString);
    //    infowindow.open(map, marker);

    //});
    
    
    google.maps.event.addDomListener(window, "resize", function () {
        $("#map-canvas").height(getMapHeight()) //setMapHeight
        $("#map-canvas").width(getMapWidth()) //setMapWidth
       
        google.maps.event.trigger(map, "resize");
       
    });
   



 

    //initialize datepicker
    $('.input-daterange').datepicker()

    //initialize show/hide for search box
    $('.expander').on('click', function () {
        $('#selectors').slideToggle();
    });

    $('#btnTopSelect').click(function () {
        $("#tree").fancytree("getTree").visit(function (node) {
                            node.setSelected(false);
                        });
                        //return false;
    })

    $('#btnHierarchySelect').click(function () {
        
                    
        $("input[name='keywords']:checked").attr('checked', false);
                
        //return false;
    })

    $("#Search").submit(function (e) {       

        resetMap()
        //prevent Default functionality
        e.preventDefault();
        e.stopImmediatePropagation();
        //var formData = getFormData();
        var path=[];
        var path = GetPathForBounds(map.getBounds())
        var area = GetAreainSqareKilometers(path)
        
        var selectedKeys = $("input[name='keywords']:checked").map(function () {
            return $(this).val();
        }).get();
        //validate inputs
        if (area > 10000000000 &&  selectedKeys.length == 0)
        {
                bootbox.alert("<h4>Current selected area is " + area + " sq km. This is too large to search for All concepts.  <br> Please limit search area to less than 10000 sq km and/or reduce search terms .<h4>")
            return
        }
        if (area > 5000 && selectedKeys.length == 1) {         
           
            if (area > 500000000000) {
                bootbox.alert("<h4>Current selected area is " + area + " sq km. This is too large to search .  <br> Please limit search area to less than 500000 sq km and/or reduce search terms .<h4>")
                return
            }
            else {
                bootbox.confirm("<h4>Current selected area is " + area + " sq km. This search can take a long time. Do you want to continue?<h4>", function () {

                    updateMap(true)
                });
            }
        }
        else {

            updateMap(true)           
        }
        // Construct the polygon.
        areaRect = new google.maps.Polygon({
            paths: path,
            strokeColor: '#FF0000',
            strokeOpacity: 0.5,
            strokeWeight: 1,
            fillColor: '#F8F8F8',
            fillOpacity: 0
        });

        areaRect.setMap(map);

        
    })
    $("#clear").on('click', function()
            {
                resetMap()
            })
    //click event for tab
    $(document).on('shown.bs.tab', 'a[data-toggle="tab"]', function (e) {
        if (e.target.id == "tableTab")
        {
            setUpTimeseriesDatatable();
            //hide sidebar
            slider.slideReveal("hide")
        }
        // activated tab
    })
    $('.data').addClass('disabled');

    //disable 
    $('body').on('click', '.disabled', function (e) {
        e.preventDefault();
        return false;
    });

   
};
  
function getMapHeight()
{
    
    toolbarHeight = $(document).height() - $(window).height();
    var mapHeight = $(document).height() - toolbarHeight + "px";
    return mapHeight;
}
function getMapWidth(panelVisible)
{    
    var panelwidth = 0;
    //panelVisible
    //if (panelVisible) 
    panelwidth = $('#slider').width();

    //var mapWidth = $(window).width() - panelwidth + "px";
    var mapWidth = $(window).width() - panelwidth + "px";
    return mapWidth;
}
function addCustomMapControls()
{
    var toggleSidePanelDiv = document.createElement('div');
    var toggleSidePanel = new toggleSidePanelControl(toggleSidePanelDiv, map);

    toggleSidePanel.index = 1;
    map.controls[google.maps.ControlPosition.RIGHT_TOP].push(toggleSidePanelDiv);

    var AreaSizeDiv = document.createElement('div');
    var AreaSize = new AreaSizeControl(AreaSizeDiv, map);

    AreaSize.index = 1;
    map.controls[google.maps.ControlPosition.BOTTOM_CENTER].push(AreaSizeDiv);
}
function addSlider()
{
    var _slider = $("#slider").slideReveal({
        width: 200,
        push: false,
        position: "right",
        top: 50,
        speed: 100,
        trigger: $("#trigger"),
        // autoEscape: false,
        show: function (obj) {
            $("#map-canvas").width(($(window).width() - $('#slider').width())) //setMapWidth
            if (typeof (map) != "undefined") google.maps.event.trigger(map, "resize");
            sidepanelVisible = true;

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
            sidepanelVisible = false;
        },
        hidden: function (obj) {
            console.log(obj);
        }
    });
    return _slider;
}
function toggleSidePanelControl(controlDiv, map)
   {

    //var controlUI = document.createElement('button');
    //controlUI.id = 'trigger';
    //controlDiv.appendChild(controlUI);

        // Set CSS for the control border
        var controlUI = document.createElement('div');
        controlUI.style.backgroundColor = 'red';
        controlUI.style.border = '1px';
        controlUI.style.borderRadius = '3px';
        controlUI.style.boxShadow = '0 2px 6px rgba(0,0,0,.3)';
        controlUI.style.cursor = 'pointer';
        controlUI.style.marginBottom = '2px';
        controlUI.style.textAlign = 'center';
        controlUI.title = 'Click to sho/hide side panel';
        controlDiv.appendChild(controlUI);
       

        // Set CSS for the control interior
        var controlText = document.createElement('button');
        controlText.style.color = 'rgb(25,25,25)';
        controlText.style.fontFamily = 'Roboto,Arial,sans-serif';
        controlText.style.fontSize = '16px';
        controlText.style.lineHeight = '25px';
        controlText.style.paddingTop = '5px';
        controlText.style.paddingLeft = '3px';
        controlText.style.paddingRight = '3px';
        controlText.style.border = '5px'
        controlText.innerHTML = '< >';
        controlText.id = 'trigger';
        controlUI.appendChild(controlText);

        // Setup the click event listeners: simply set the map to
    // Chicago

        google.maps.event.addDomListener(controlUI, 'click', function () {
            if (sidepanelVisible) { slider.slideReveal("hide") }
            else { slider.slideReveal("show") }
        });

        //if (typeof slider != "undefined") { // Show
       
        //} else { // Hide
        //    self.slideReveal("hide");
        //}
}
function AreaSizeControl(controlDiv, map) {

    //var controlUI = document.createElement('button');
    //controlUI.id = 'trigger';
    //controlDiv.appendChild(controlUI);
    //div{
    //    -moz-border-radius:10px;
    //    -webkit-border-radius:10px;
    //    border-radius:10px;
    //    background: #fff; /* fallback for browsers that don't understand rgba */
    //    border: #solid 10px #000; /* fallback for browsers that don't understand rgba */
    //    background-color: rgba(255,255,255,0.8)/* slighly transparent white */
    //    border-color: rgba(0,0,0,0.2) /*Very transparent black*/
    //}

    // Set CSS for the control border
    var controlUI = document.createElement('div');
    controlUI.style.backgroundColor = 'rgb(250, 250, 250)';
    controlUI.style.border = '1px';
    controlUI.style.borderRadius = '5px';
    controlUI.style.boxShadow = '0 2px 6px rgba(0,0,0,.3)';
    controlUI.style.cursor = 'pointer';
    controlUI.style.marginBottom = '12px';
    controlUI.style.textAlign = 'center';
    controlUI.title = 'Current size of map area';
    controlDiv.appendChild(controlUI);


    // Set CSS for the control interior
    var controlText = document.createElement('div');
    controlText.style.color = 'rgb(125, 125, 125)';
    controlText.style.fontFamily = 'Roboto,Arial,sans-serif';
    controlText.style.fontSize = '12px';
    controlText.style.lineHeight = '25px';
    controlText.style.paddingTop = '0px';
    controlText.style.paddingLeft = '3px';
    controlText.style.paddingRight = '3px';
    controlText.style.paddingBottom = '0px';
    controlText.style.borderRadius = '15px';
    controlText.style.backgroundColor = 'rgba(255,255,255,0.8)'/* slighly transparent white */
    controlText.style.border = '5px'
    controlText.innerHTML = "Area in sq km";
    controlText.id = 'MapAreaControl';
    controlUI.appendChild(controlText);

    // Setup the click event listeners: simply set the map to
    // Chicago

    //google.maps.event.addListener(map, 'zoom_changed', function () {
    //    var path = GetPathForBounds(map.getBounds())
    //    var area = GetAreainSqareKilometers(path)
    //    controlText.innerHTML = area;
    //});
        

    

    //if (typeof slider != "undefined") { // Show

    //} else { // Hide
    //    self.slideReveal("hide");
    //}
}
function addLocationSearch()
{
    var input = /** @type {HTMLInputElement} */(
      document.getElementById('pac-input'));

    //var marker = new google.maps.Marker({
    //    map: map,
    //    anchorPoint: new google.maps.Point(0, -29)
    //});
    var types = document.getElementById('type-selector');
    map.controls[google.maps.ControlPosition.TOP_LEFT].push(input);
    map.controls[google.maps.ControlPosition.TOP_LEFT].push(types);

    var autocomplete = new google.maps.places.Autocomplete(input);
    autocomplete.bindTo('bounds', map);


    google.maps.event.addListener(autocomplete, 'place_changed', function() {
        //infowindow.close();
        //marker.setVisible(false);
        var place = autocomplete.getPlace();
        if (!place.geometry) {
            return;
        }

        // If the place has a geometry, then present it on a map.
        if (place.geometry.viewport) {
            map.fitBounds(place.geometry.viewport);
        } else {
            map.setCenter(place.geometry.location);
            map.setZoom(14);  // Why 17? Because it looks good.
        }
        //marker.setIcon(/** @type {google.maps.Icon} */({
        //    url: place.icon,
        //    size: new google.maps.Size(71, 71),
        //    origin: new google.maps.Point(0, 0),
        //    anchor: new google.maps.Point(17, 34),
        //    scaledSize: new google.maps.Size(35, 35)
        //}));
        //marker.setPosition(place.geometry.location);
        //marker.setVisible(true);

        var address = '';
        if (place.address_components) {
            address = [
              (place.address_components[0] && place.address_components[0].short_name || ''),
              (place.address_components[1] && place.address_components[1].short_name || ''),
              (place.address_components[2] && place.address_components[2].short_name || '')
            ].join(' ');
        }

        //infowindow.setContent('<div><strong>' + place.name + '</strong><br>' + address);
        //infowindow.open(map, marker);
    });

    // Sets a listener on a radio button to change the filter type on Places
    // Autocomplete.
    function setupClickListener(id, types) {
        var radioButton = document.getElementById(id);
        google.maps.event.addDomListener(radioButton, 'click', function() {
            autocomplete.setTypes(types);
        });
    }

    setupClickListener('changetype-all', []);
    //setupClickListener('changetype-address', ['address']);
    //setupClickListener('changetype-establishment', ['establishment']);
    setupClickListener('changetype-geocode', ['geocode']);
}
function getMapAreaSize()
{
    var path = GetPathForBounds(map.getBounds())
    var area = GetAreainSqareKilometers(path)
    return 'Area: ' + area + ' sq km';
}
function resetMap()
{
    deleteClusteredMarkersOverlays();
    $('.data').addClass('disabled');
    resetUserSelection()
    if (typeof areaRect != "undefined") areaRect.setMap(null);
}
function processMarkers(geoJson)
{
    //map.data.loadGeoJson('https://storage.googleapis.com/maps-devrel/google.json');

    var json = JSON.parse(geoJson);
    //map.data.addGeoJson(geojson);
    //zoom(map);
    if (json != null) {
        if (json.features.length == 0)
        {
            bootbox.alert(" No timeseries for specified parameters found.")
            return;
        }
        // set title
        //$("#headerlbl").html(programname);

        //set legend html
       // var s = json.properties.legend;

        //if (s.length > 0) {
        //    $("#openlegend").html(s);
        //}

        //features = json.features;
        if (typeof json.features != "undefined") {

            $('#tableTab').removeClass('disabled')

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
    var formdata = []//$("#Search").serializeArray()

    var bounds = map.getBounds();
    var ne = bounds.getNorthEast(); // LatLng of the north-east corner
    var sw = bounds.getSouthWest(); // LatLng of the south-west corder 
    var startDate = $("#startDate").val()
    var endDate = $("#endDate").val()
    var keywords = new Array();
    //variable.push  = $("#variable").val()

   //selected Services
    var services = mySelectedServices;
    //alert(myTimeSeriesClusterDatatable.rows('.selected').data().length + ' row(s) selected');
    //only MuddyRiver
    //services.push(181);
    var selectedKeys = $("input[name='keywords']:checked").map(function () {
        return $(this).val();
    }).get().join("##");

   


    if (selectedKeys.length != 0) {
        keywords.push(selectedKeys)
    }
    else
        {
        var tree = $("#tree").fancytree("getTree");

        var selKeys = $.map(tree.getSelectedNodes(), function (node) {
            
            if (node.children !== null)
            {
                 $.map(node.children, function (node) {
                        return  node.title ;
                    });
            }
            else 
            {
                return node.title;
            }
                       

        }).join("##");
        //var selKeys = $.map(selNodes, function (node) {
        //    return  node.title ;
        //});
        

        //var selRootNodes  = $.map(tree.getSelectedNodes(true));
        //var selRootKeys = $.map(selRootNodes, function(node){
        //    return node.key;
        //});
        keywords.push(selKeys)
                
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
    formdata.push({ name: "sessionGuid", value: sessionGuid })

    //var formdata = $("#Search").serializeArray()

    return formdata;
}


//upddate map wit new clusters
function updateMap(isNewRequest) {
    
  
    if (clusteredMarkersArray.length == 0 && isNewRequest == false) return;//only map navigation
    $("#pageloaddiv").show();
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
            //setUpTimeseriesDatatable();
            $('.data').removeClass('disabled');
            $("#pageloaddiv").hide();
        },
        error: function (xmlhttprequest, textstatus, message) {
            serviceFailed(xmlhttprequest, textstatus, message)
            $("#pageloaddiv").hide();
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
            icon: new google.maps.MarkerImage(clusterMarkerPath + 'm6_single.png', new google.maps.Size(32, 32), null, null, new google.maps.Size(32, 32)),
            //icon: new google.maps.MarkerImage(markerPath + 'm6_single.png', new google.maps.Size(53, 52), null, new google.maps.Point(icon_width / 2, icon_width / 2), new google.maps.Size(icon_width, icon_width)),

            //icon: new google.maps.MarkerImage(/Content.png', new google.maps.Size(32, 32), null, null, new google.maps.Size(28, 28)),
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
            //var c = getDetailforCluster(id, clusterid)
            //infoWindow.setContent(c);
            //infoWindow.open(map, this);
            //$('#example').DataTable();
            setUpDatatables(clusterid);
            $('#SeriesModal').modal('show')
            //var details = getDetailsForMarker(clusterid)
            //createInfoWindowContent()

        });
        //var infoBox = new InfoBox({ latlng: marker.getPosition(), map: map });

    }

    else {
        if (count < 5) {
            icon_choice = 0;
            icon_height = 28;
            icon_width = 32;
        }
        else if (count < 10) {
            icon_choice = 0;
            icon_height = 30;
            icon_width = 34;
        }
        else if (count < 25) {
            icon_choice = 1;
            icon_height = 32;
            icon_width = 36;
        }
        else if (count < 50) {
            icon_choice = 2;
            icon_height = 36;
            icon_width = 38;
        }
        else if (count < 100) {
            icon_choice = 3;
            icon_height = 40;
            icon_width = 40;
        }
        else if (count < 250) {
            icon_choice = 3;
            icon_height = 44;
            icon_width = 44;
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
            icon: new google.maps.MarkerImage(clusterMarkerPath + "m6.png", new google.maps.Size(53, 52), null, new google.maps.Point(icon_width / 2, icon_width / 2), new google.maps.Size(icon_width, icon_width)),
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
            $('#SeriesModal').modal('show')
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

function setupServices()
{

    $.fn.DataTable.isDataTable("#dtServices")
    {
        $('#dtServices').DataTable().clear().destroy();
    }

    var actionUrl = "/home/getServiceList"
    // $('#demo').html( '<table cellpadding="0" cellspacing="0" border="0" class="display" id="example"></table>' );
 
    myServicesDatatable = $('#dtServices').dataTable({
        "ajax": actionUrl,
         "columns": [
            { "data": "ServiceID" },
            { "data": "ServiceCode" },
            { "data": "Title" },
            { "data": "DescriptionUrl", "visible": false },
            { "data": "ServiceUrl", "visible": false },
            { "data": "Checked", "visible": false },
            { "data": "Organization" },
            { "data": "Sites", "visible": false },
            { "data": "Variables", "visible": false },
            { "data": "Values", "visible": false },
            { "data": "ServiceBoundingBox", "visible": false}
         ],
         //"rowCallback": function( row, data ) {
         //    if ( $.inArray(data.DT_RowId, mySelectedServices) !== -1 ) {
         //        $(row).addClass('selected');
         //    }
         //},
         "createdRow": function ( row, data, index ) {

             var id = $('td', row).eq(0).html();
             var title = $('td', row).eq(2).html();
             var url = 'http://hiscentral.cuahsi.org/pub_network.aspx?n=';
             $('td', row).eq(2).html("<a href='" + url + id + "' target='_Blank'>" + title + " </a>");


         },
         "scrollX": true,
         initComplete: function () {
             this.fnAdjustColumnSizing();
         }
    })
    $('a.toggle-vis').on('click', function (e) {
        e.preventDefault();

        // Get the column API object
        var column = table.column($(this).attr('data-column'));

        // Toggle the visibility
        column.visible(!column.visible());
    });


    $('#dtServices tbody').on('click', 'tr', function () {
        $(this).toggleClass('selected');
        var id = this.cells[0].innerHTML;
        if ($.inArray(id, mySelectedServices ) == -1)
        {
            //add
            mySelectedServices.push(id);
        }
        else {            

            //remove
            mySelectedServices = $.grep(mySelectedServices, function (element, index) {
                return element.id == id;
            })
        }


    });

    $('#saveServiceSelection').click(function () {
       // alert(myServicesDatatable[0].rows('.selected').data().length + ' row(s) selected');
       
    });
    $('#cancelServiceSelection').click(function () {
        mySelectedServices.length = 0;

    });
    //return table;
}

function setUpDatatables(clusterid)
{
    
//    var dataSet = [
//    ['Trident','Internet Explorer 4.0','Win 95+','4','X'],
//    ['Trident','Internet Explorer 5.0','Win 95+','5','C'],
 
    //];
    $.fn.DataTable.isDataTable("#dtMarkers")
    {
        $('#dtMarkers').DataTable().clear().destroy();
    }
    

    //var dataSet = getDetailsForMarker(clusterid)
    var actionUrl = "/home/getDetailsForMarker/" + clusterid
   // $('#example').DataTable().clear()
   // $('#demo').html( '<table cellpadding="0" cellspacing="0" border="0" class="display" id="example"></table>' );
 
    var oTable= $('#dtMarkers').dataTable({
        "ajax": actionUrl,
        "autoWidth": true,
        "jQueryUI": false,
        "deferRender": true,
         "dom": 'C<"clear">lfrtip',
         colVis: {
             //restore: "Restore",
             //showAll: "Show all",
             //showNone: "Show none",
             activate: "mouseover",
             exclude: [0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,16,17,18,19,20,21],
             groups: [
                //{
                //    title: "Main",
                //    columns: [ 0, 1,4, 5,6,7,9]
                //},
                {
                    title: "Show All Columns",
                    columns: [ 2, 3, 8, 9,10, 11, 12, 13, 14, 15, 16, 16, 17, 18, 19, 20, 21]
                }
             ]
         },
         "columns": [
             
            { "data": "SeriesId" },
            { "data": "ServCode", "sTitle": "Service Name" },
            { "data": "ServURL", "visible": false },
            { "data": "SiteCode", "visible": false },
            { "data": "VariableCode", "visible": false },
            { "data": "VariableName"},
            { "data": "BeginDate" },
            { "data": "EndDate" },
            { "data": "ValueCount", "visible": false },
            { "data": "SiteName" },
            { "data": "Latitude", "visible": false },
            { "data": "Longitude", "visible": false },
            { "data": "DataType", "visible": false },
            { "data": "ValueType", "visible": false },
            { "data": "SampleMedium","visible": false },
            { "data": "TimeUnit", "visible": false },
            { "data": "GeneralCategory", "visible": false },
            { "data": "TimeSupport", "visible": false },
            { "data": "ConceptKeyword", "visible": false },
            { "data": "IsRegular", "visible": false },
            { "data": "VariableUnits","visible": false },
            { "data": "Citation", "visible": false }            
        ],
        
         "scrollX": true, //removed to fix column alignment 
         
         "createdRow": function (row, data, index) {

             //if (data[0].replace(/[\$,]/g, '') * 1 > 250000) {

             var id = $('td', row).eq(0).html();
             //var d = $('td', row).eq(4).html();
             //$('td', row).eq(0).append("<a href='/Export/downloadFile/" + id + "' id=" + id + " <span class='glyphicon glyphicon-download-alt'  aria-hidden='true'> </span> </a>");
             $('td', row).eq(0).append("<a href='/Export/downloadFile/" + id + "' id=" + id + "<span><img  src='/Content/Images/download-icon-25.png' ></span> </a>");
             $('td', row).eq(0).click(function () {
                 //downloadtimeseries('csv', id); return false;/Content/Images/ajax-loader-green.gif
                 //$('#spinner' + id).removeClass('hidden')
                 $(this).append("<span><img class='spinner' src='/Content/Images/ajax-loader-green.gif'></span>");
                 //$.fileDownload($(this).prop('href'), {
                 //    preparingMessageHtml: "We are preparing your report, please wait...",
                 //    failMessageHtml: "There was a problem generating your report, please try again."
                 //})
                 
                 $.fileDownload($(this).prop('href'))                     
                     .done(function () {
                         $('.spinner').addClass('hidden');
                         $('.spinner').parent().parent().addClass('selected');

                     })
                     .fail(function () {
                         $('.spinner').addClass('hidden');
                         $('.spinner').parent().parent().addClass('downloadFail');
                     });

             });


             //}
         },
        initComplete: function () {
            var api = this.api();
           
            api.columns().indexes().flatten().each(function (i) {
                if (i > 5 || i == 0) return;
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
 
                column.data().unique().sort().each(function (d, j) {
                    
                    select.append( '<option value="'+d+'">'+d+'</option>' )
                } );
            });
            oTable.fnAdjustColumnSizing();
        }
        
          

        //"retrieve": true
    });

    //##########Context menu
    //$(document).contextmenu({
    //    on: ".dataTable tr",
    //    menu: [
    //      { title: "Download as CSV", cmd: "DownloadAsCSV" },
    //      { title: "Add to DataCart", cmd: "AddtoDataCart" }
    //    ],
    //    select: function (event, ui) {
    //        var celltext = ui.target.text();
           

    //        var colvindex = ui.target.parent().children().index(ui.target);
    //        var colindex = $('table thead tr th:eq(' + colvindex + ')').data('column-index');
    //        switch (ui.cmd) {
    //            case "DownloadAsCSV":
    //                var seriesId = ui.target.parent().children()[0].innerText;
    //                var cell = ui.target.parent().children()[0];
    //                //cell.cssClass("activeDownload");
    //                ui.target.parent().addClass('selected');
    //                $('.dataTable tr:eq(1)').addClass(".activeDownload")
    //                     url = "/Export/downloadFile/" + seriesId
    //                     var _iframe_dl = $('<iframe  onerror="alert()" />')
    //                             .attr('src', url)
    //                             .hide()                                 
    //                             .appendTo('body');
    //                break;
    //            case "AddtoDataCart":
    //                alert("Datacart")
    //                break;
    //        }
    //    },
    //    beforeOpen: function (event, ui) {
    //        var $menu = ui.menu,
    //            $target = ui.target,
    //            extraData = ui.extraData;
    //        ui.menu.zIndex(9999);
    //    }
    //});
    //////////#################

    //$('#dtMarkers tbody').on('click', 'tr', function () {

    //     var name = $('td', this).eq(0).text();
    //     var id = this.cells[0].innerHTML;
    //     url = "/Export/downloadFile/" + id

    //     //bootbox.confirm("Are you sure?", function (url) {

    //         var _iframe_dl = $('<iframe />')
    //             .attr('src', url)
    //             .hide()
    //             .appendTo('body');
    //     //});                         
    //});


    $('#dtMarkers tbody').on('click', 'tr', function () {
        //$(this).addClass('selected');

    });
    //####
   
    //######
    //$('#dtMarkers tbody').contextmenu({
    //    target: '#context-menu2',
    //    onItem: function (context, e) {
    //        alert($(e.target).text());
    //    }
    //});

    //$('#context-menu2').on('show.bs.context', function (e) {
    //    console.log('before show event');
    //});

    //$('#context-menu2').on('shown.bs.context', function (e) {
    //    console.log('after show event');
    //});

    //$('#context-menu2').on('hide.bs.context', function (e) {
    //    console.log('before hide event');
    //});

    //$('#context-menu2').on('hidden.bs.context', function (e) {
    //    console.log('after hide event');
    //});


    myTimeSeriesClusterDatatable = $('#dtMarkers').DataTable()  

    //$('#Download').click(function () {
    //    download();
    //});

    //$('#DownloadAsCSV').click(function () {

    //    var name = $('td', this).eq(0).text();
    //    //     var id = this.cells[0].innerHTML;

    //    if (myTimeSeriesClusterDatatable.rows('.selected').data().length > 0)
    //    {
    //        var list = new Array();
    //        var rows = myTimeSeriesClusterDatatable.rows('.selected').data();

          


    //        //<th>ServCode</th>
    //        //                <th>ServURL</th>
    //        //                <th>SiteCode</th>
    //        //                <th>VariableCode</th>
    //        //                <th>VariableName</th>
    //        //                <th>BeginDate</th>
    //        //                <th>EndDate</th>

    //        for (i = 0; i < rows.length; i++)
    //        {
    //            list[i] = new Array(
                        
    //                    rows[i].ServCode,
    //                    rows[i].ServURL,
    //                    rows[i].SiteCode,
    //                    rows[i].VariableCode,
    //                    rows[i].SiteName,
    //                    rows[i].VariableName,
    //                    rows[i].BeginDate,
    //                    rows[i].EndDate,
    //                    rows[i].ValueCount,                        
    //                    rows[i].Latitude,
    //                    rows[i].Longitude,
    //                    rows[i].DataType,
    //                    rows[i].ValueType,
    //                    rows[i].SampleMedium,
    //                    rows[i].TimeUnit,
    //                    rows[i].GeneralCategory,
    //                    rows[i].TimeSupport,
    //                    rows[i].ConceptKeyword,
    //                    rows[i].IsRegular,
    //                    rows[i].VariableUnits,
    //                    rows[i].Citation
    //                );
    //        }

    //        $.ajax({
    //            url: "/Export/downloadFile/1",
    //            //url: "/api/seriesdata?SeriesID=1",
    //            type: 'Post',
    //            dataType: 'json',
    //            timeout: 60000,
    //            processData: false,
    //            //data: list,
    //            //success: function () {
    //            //    //alert("ys")
    //            //},
    //            //error: function (xmlhttprequest, textstatus, message) {
    //            //    serviceFailed(xmlhttprequest, textstatus, message)
    //            //}
    //        }).done(function (d)
    //        { bootbox.alert(d) }
    //        );
    //        //var newWindow = window.open('/Home/CreatePartialView', '_blank', 'left=100,top=100,width=400,height=300,toolbar=1,resizable=0');         
    //    }
    //    else
    //        alert("Please select Series")
    //});
}

function setUpTimeseriesDatatable()
{
    if (clusteredMarkersArray.length == 0)
    {
        return;
    }
    // $('#dtTimeseries').(':visible')
    $('#dtTimeseries').removeClass("hidden");

    $.fn.DataTable.isDataTable("#dtTimeseries")
    {
        $('#dtTimeseries').DataTable().clear().destroy();
    }


    //var dataSet = getDetailsForMarker(clusterid)
    var actionUrl = "/home/getTimeseries"
    // $('#example').DataTable().clear()
    // $('#demo').html( '<table cellpadding="0" cellspacing="0" border="0" class="display" id="example"></table>' );
    var oTable = $('#dtTimeseries').dataTable({
        "ajax": actionUrl,
        "dom": 'C<"clear">lfrtip',
        "deferRender": true,
        "columns": [
           { "data": "SeriesId" },
           { "data": "ServCode", "sTitle": "Service Name" },
           { "data": "ServURL", "visible": false },
           { "data": "SiteCode", "visible": false },
           { "data": "VariableCode", "visible": false },
           { "data": "VariableName" },
           { "data": "BeginDate" },
           { "data": "EndDate" },
           { "data": "ValueCount", "visible": false },
           { "data": "SiteName" },
           { "data": "Latitude", "visible": false },
           { "data": "Longitude", "visible": false },
           { "data": "DataType", "visible": false },
           { "data": "ValueType", "visible": false },
           { "data": "SampleMedium", "visible": false },
           { "data": "TimeUnit", "visible": false },
           { "data": "GeneralCategory", "visible": false },
           { "data": "TimeSupport", "visible": false },
           { "data": "ConceptKeyword", "visible": false },
           { "data": "IsRegular", "visible": false },
           { "data": "VariableUnits", "visible": false },
           { "data": "Citation", "visible": false }
        ],
        "createdRow": function (row, data, index) {

            //if (data[0].replace(/[\$,]/g, '') * 1 > 250000) {

            var id = $('td', row).eq(0).html();
            //var d = $('td', row).eq(4).html();
            $('td', row).eq(0).append("<a href='/Export/downloadFile/" + id + "' id=" + id + "<span><img  src='/Content/Images/download-icon-25.png' ></span> </a>");
            $('td', row).eq(0).click(function () {
                //downloadtimeseries('csv', id); return false;/Content/Images/ajax-loader-green.gif
                //$('#spinner' + id).removeClass('hidden')
                $(this).append("<span><img class='spinner' src='/Content/Images/ajax-loader-green.gif' style='padding-left:4px;padding-right:4px'></span>");
                //$.fileDownload($(this).prop('href'), {
                //    preparingMessageHtml: "We are preparing your report, please wait...",
                //    failMessageHtml: "There was a problem generating your report, please try again."
                //})

                $.fileDownload($(this).prop('href'))
                    .done(function () {
                        $('.spinner').addClass('hidden');
                        $('.spinner').parent().parent().addClass('selected');

                    })
                    .fail(function () {
                        $('.spinner').addClass('hidden');
                        $('.spinner').parent().parent().addClass('downloadFail');
                    });

            });
        }        
    });
      

}

function downloadtimeseries(format, id)
{
    //var name = $('td', this).eq(0).text();
    //var id = this.cells[0].innerHTML;
    url = "/Export/downloadFile/" + id


    $.ajax({
        url: url,
        data:  null,
        success: function (data) {
            //Do something with data that is returned. (The downloaded file?)
            alert();
        }
    });

    //bootbox.confirm("Are you sure?", function (url) {

    //var _iframe_dl = $('<iframe onload="testalert()";/>')
    //    .attr('src', url)
    //    //.hide()
    //    .appendTo('body')
        
    //;
    //});                         
   
}

function resetUserSelection() {

}

function testalert()
{
    alert("a");
}

function fnGetSelected(oTableLocal) {
    var aReturn = new Array();
    oTableLocal.$("tr").filter(".row_selected").each(function (index, row) {
        aReturn.push(row);// this should work, if not try aReturn.push($(row));
        //to get the information in the first column 
        aReturn.push($(row).eq(0).text());
        return aReturn;
    })
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
    bootbox.alert('Service call failed with Error ' + xmlhttprequest.statusText + ' Please limit search extent, date range or Concepts.');
    //clean up
    resetMap()
    //}

};

function GetAreainAcres(poly) {
    var result = parseFloat(google.maps.geometry.spherical.computeArea(poly.getPath())) * 0.000247105;
    return result.toFixed(4);
}

function GetAreainSqareKilometers(path) {
    
    var result = parseFloat(google.maps.geometry.spherical.computeArea(path)) * 0.0000001;
    //if (_result > 10000) result = result.toFixed(0);
    if (result < 100) { return result.toFixed(3); }
    if (result < 1000) { return result.toFixed(2); }
    if (result < 10000) { return result.toFixed(1); }
    return result.toFixed(0);
}
function GetPathForBounds(bounds)
{
    var ne = bounds.getNorthEast(); // LatLng of the north-east corner
    var sw = bounds.getSouthWest(); // LatLng of the south-west corder 

    var xMin = Math.min(ne.lng(), sw.lng())
    var xMax = Math.max(ne.lng(), sw.lng())
    var yMin = Math.min(ne.lat(), sw.lat())
    var yMax = Math.max(ne.lat(), sw.lat())

    var path = [];
    path.push (ne)
    path.push(new google.maps.LatLng(sw.lat(),ne.lng()))
    path.push(sw)
    path.push(new google.maps.LatLng(ne.lat(),sw.lng()))
    return path;
}


