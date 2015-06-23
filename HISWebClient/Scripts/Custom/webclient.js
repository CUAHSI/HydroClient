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
var randomId;
var currentPlaceAddress;
var timeSeriesRequestStatus;
var slider;
var sidepanelVisible = false;

var selectedTimeSeriesMax = 25;

//BC - 19-Jun-2015 - Disable concept counting - possible later use...
//var selectedConceptsMax = 4;

//lisy of services that only have 
var ArrayOfNonObservedServices = ["84","187","189","226","262","267","274"]

var taskCount = 0;

$(document).ready(function () {

    $("#pageloaddiv").hide();
    initialize();
   
})

function initialize() {

    var myCenter = new google.maps.LatLng(39, -92); //us
    //var myCenter = new google.maps.LatLng(42.3, -71);//boston
   // var myCenter = new google.maps.LatLng(41.7, -111.9);//Salt Lake
    
      //BC - disable infoWindow for now...
//    infoWindow = new google.maps.InfoWindow();

    //init list od datatables for modal 
    myServicesList = setupServices();

    var mapProp = {
        center: myCenter,
        zoom: 5,
        draggable: true,
        scrollwheel: true,
        mapTypeId: google.maps.MapTypeId.ROADMAP,
        mapTypeControl: true,
        scaleControl: true,
        overviewMapControl: true,
        overviewMapControlOptions: {
            opened: true },     
        zoomControl: true,
        panControl:false,        
        zoomControlOptions: {     
            //style: google.maps.ZoomControlStyle.SMALL,
            position: google.maps.ControlPosition.LEFT_TOP
        },
        mapTypeControlOptions: {
            style: google.maps.MapTypeControlStyle.DEFAULT,
            position: google.maps.ControlPosition.LEFT_BOTTOM,
            mapTypeIds: [google.maps.MapTypeId.ROADMAP, google.maps.MapTypeId.HYBRID, google.maps.MapTypeId.TERRAIN]
        },
    };

    //get refernce to map sizing of window happens later to prevent gap   
    map = new google.maps.Map(document.getElementById("map-canvas"), mapProp);
    

    //UI
    
    addCustomMapControls();

    slider = addSlider();
 
    slider.slideReveal("show");
    sidepanelVisible = true;
  
    addLocationSearch();
    
    //triger update of map on these events
    google.maps.event.addListener(map, 'dblclick', function () {
        if ((clusteredMarkersArray.length > 0)) {
            updateMap(false)
            //$("#MapAreaControl").html(getMapAreaSize());
        }
    });
    google.maps.event.addListener(map, 'dragend', function () {
        if ((clusteredMarkersArray.length > 0)) {
            updateMap(false)
            //$("#MapAreaControl").html(getMapAreaSize());
        }
    });
    
    google.maps.event.addListener(map, 'zoom_changed', function () {
        if ((clusteredMarkersArray.length > 0)) {
            updateMap(false)
            
            //$("#MapAreaControl").html(getMapAreaSize());
            
        }
    });
    //added to load size on startup
    google.maps.event.addListener(map, 'bounds_changed', function () {
        //if ((clusteredMarkersArray.length > 0)) {
            //updateMap(false)

            $("#MapAreaControl").html(getMapAreaSize());

        //}
    });

    //google.maps.event.addListener(marker, 'click', function () {

    //    infowindow.setContent(contentString);
    //    infowindow.open(map, marker);

    //});
    
    
    google.maps.event.addDomListener(window, "resize", function () {
        $("#map-canvas").height(getMapHeight()) //setMapHeight
        $("#map-canvas").width(getMapWidth()) //setMapWidth
       
        google.maps.event.trigger(map, "resize");
        if (sidepanelVisible)
        {
            slider.slideReveal("show")
        }
    });
   

    //initialize datepicker
    $('.input-daterange').datepicker();

    //Set start and end of date range...
    var initDate = new Date();

    $('#startDate').val(('0' +(initDate.getMonth() + 1).toString().slice(-2)) + '/' + initDate.getDate().toString() + '/' + (initDate.getFullYear() - 1).toString());
    $('#endDate').val(('0' +(initDate.getMonth() + 1).toString().slice(-2)) + '/' + initDate.getDate().toString() + '/' + (initDate.getFullYear()).toString());

    //Button click handler for Select Date Range...
    $('#btnDateRange').on('click', function (event) {
        //Assign current start and end date values to their modal counterparts...
        $('#startDateModal').val($('#startDate').val());
        $('#endDateModal').val($('#endDate').val());
    });

    //Button click handler for Date Range Modal Save 
    $('#btnDateRangeModalSave').on('click', function (event) {
        //Assign current start and end date values to their modal counterparts...
       
        var startDateModal = $("#startDateModal").val()
        if (checkReg2(startDateModal) == false) { bootbox.alert("Please validate your From: date"); return }
        $('#startDate').val($('#startDateModal').val());

        var endDateModal = $("#endDateModal").val()
        if (checkReg2(endDateModal)==false) { bootbox.alert("Please validate your To: date"); return }
        $('#endDate').val($('#endDateModal').val());
    });

    //initialize show/hide for search box
    $('.expander').on('click', function () {
        $('#selectors').slideToggle();
    });

 
    //allocate a RandomId instance...
   randomId = new RandomId( { 'iterationCount': 10,
                              'characterSets': ['alpha', 'numeric']
                            } );

    //Allocate a TimeSeriesRequestStatus instance...
   timeSeriesRequestStatus = (new TimeSeriesRequestStatus()).getEnum();

    //Download All button click handler..
   $('#btnDownloadAll').on('click', function (event) {
       //For each zip blob download button element...
       $("tr > td > button.zipBlobDownload").each(function (index) {
           //Initiate individual file downloads at three second intervals
           var buttonThis = this;
           setTimeout(function () {
               //$(buttonArray[index]).click();
               $(buttonThis).click();
           }, (3000 * index));
       });
   });

    //BC - 19-Jun-2015 - Disable concept counting - possible later use...
    //Click handler for 'Most Common' concept checkboxes...
   //$('.topCategories').click(function (event) {
    
   //    if (!$(event.target).prop('checked')) {
   //        //Checkbox unchecked - enable all unchecked checkboxes
   //        ($("input[name='keywords']").not(':checked')).prop( "disabled", false );
   //    }
   //    else {
   //        //Checkbox checked - if maximum reached, disable all unchecked checboxes
   //        var checked = $("input[name='keywords']:checked");
   //        var length = checked.length;

   //        if (selectedConceptsMax <= length) {
   //            ($("input[name='keywords']").not(':checked')).prop("disabled", true);
   //        }
   //     }
   //});

   $('#btnTopSelect').click(function () {

       updateKeywordList("Common");       

    });

    $('#btnHierarchySelect').click(function () {
                        
        updateKeywordList();
        //if (0 < length) {
        //    //Certain concepts selected...
        //    for (var i = 0; i < length; ++i) {

        //        if (selectedNodes[i].children !== null)//all children are selected
        //        {
        //            //$.map(node.children, function (node) {
        //            //       return  node.title ;
        //            //});
        //            allChildrenSelected = true;
        //            list.append('<li>' + selectedNodes[i].title + '</li>');
                    

        //        }
        //        if (!allChildrenSelected) {
        //            list.append('<li>' + selectedNodes[i].title + '</li>');
        //        }
                
        //    }
        //}
        //else {
        //    //All concepts selected...
        //    list.append('All');
        //}
    });

    $("input[name='checkOnlyObservedValues']").change(function (e) {

        var rows = $("#dtServices").dataTable().fnGetNodes();
        //reset global array
        mySelectedServices = [];

        if ($("input[name='checkOnlyObservedValues']").is(':checked')) {         
           
            
            for (var i = 0; i < rows.length; i++) {
                // Get HTML of 3rd column (for example)
                //get vvalue from (network)id field 
                var id = $(rows[i]).find("td:eq(5)").html();
                //check if value in array of non observed services
                if ($.inArray(id, ArrayOfNonObservedServices) == -1) {

                    //add
                    mySelectedServices.push(id);
                    //mark with select class
                    $(rows[i]).addClass('selected');
                }
            }
        }
        else
        {
            for (var i = 0; i < rows.length; i++) {
                // Get HTML of 3rd column (for example)
                
                //check if value in array of non observed services
                

                    
                    //mark with select class
                    $(rows[i]).removeClass('selected');
                
            }
        }
        updateServicesList();
    });

    $("#Search").submit(function (e) {       

        resetMap()
        //prevent Default functionality
        e.preventDefault();
        e.stopImmediatePropagation();
        //var formData = getFormData();
        var path=[];
        path = GetPathForBounds(map.getBounds())
        var area = GetAreainSqareKilometers(path)
        

        var selectedKeys = $("input[name='keywords']:checked").map(function () {
            return $(this).val();
        }).get();
           
        var list = $('#olConcepts');

        if (!validateQueryParameters(area, selectedKeys)) return;

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
    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
         if (e.target.id == "tableTab") {
             //Hide the zendesk iframe...
             $('#launcher').hide();

             setUpTimeseriesDatatable();
            var table = $('#dtTimeseries').DataTable();
            table.order([0, 'asc']).draw();
            ////hide sidebar
            // slider.slideReveal("hide")

            }
         if (e.target.id == "mapTab") {
             //Show the zendesk iframe...
             $('#launcher').show();

             google.maps.event.trigger(map, "resize");
             if (!sidepanelVisible) {
                 slider.slideReveal("show")
             }
    
        }
        // activated tab
    })

    $('.data').addClass('disabled');

    //disable 
    $('body').on('click', '.disabled', function (e) {
        e.preventDefault();
        return false;
    });

    $("#map-canvas").height(getMapHeight()) //setMapHeight
    $("#map-canvas").width(getMapWidth) //setMapWidth
    google.maps.event.trigger(map, "resize");
};
  
function validateQueryParameters(area, selectedKeys) {
    //validate inputs

    if (area > 100000) {
        bootbox.alert("<h4>Current selected area is " + area + " sq km. This is too large to search.  <br> Please limit search area to less than 100000 sq km and/or reduce search keywords.<h4>")
        return false;
    }

    if (area > 25000 && selectedKeys.length == 0) {
        bootbox.alert("<h4>Current selected area is " + area + " sq km. This is too large to search for <strong>All</strong> keywords.  <br> Please limit search area to less than 25000 sq km and/or reduce search keywords.<h4>")
        return false;
    }
    if (area > 25000 && selectedKeys.length == 1) {

        //if (area > 50000) {
        //    bootbox.alert("<h4>Current selected area is " + area + " sq km. This is too large to search.  <br> Please limit search area to less than 50000 sq km and/or reduce search keywords.<h4>")
        //    return false;
        //}
        //else {
            bootbox.confirm("<h4>Current selected area is " + area + " sq km. This search can take a long time. Do you want to continue?<h4>", function () {

                updateMap(true)
            });
        //}
    }
    else {

        if (area > 25000 && selectedKeys.length > 1) {
            bootbox.alert("<h4>Current selected area is " + area + " sq km and you have cselecte more than 1 keywords. Please reduce area or number of keywords to 1?<h4>", function () {

                return false;
            });
        }
        if (area < 25000 && selectedKeys.length > 1) {
            bootbox.confirm("<h4>Current selected area larger than 2500 sq km and you selected several keywords. This search can take a long time and might timeout. Do you want to continue?<h4>", function () {

                updateMap(true)
            });
            //if (area > 100000 && selectedKeys.length > 5) { 
            //    bootbox.confirm("<h4>Current selected area is " + area + " sq km. This search can take a long time. Do you want to continue?<h4>", function () {

            //        updateMap(true) 
            //    });

               
        }
        else
        {
            updateMap(true)
        }
        
    }
    return true;
}

function updateKeywordList(type) {
    if (type == "Common") {
        //Unset/enable all 'Full' tab entries...
        $("#tree").fancytree("getTree").visit(function (node) {
            node.setSelected(false);
            node.unselectable = false;
            node.hideCheckbox = false;
        });
        //return false;

        //Clear and re-populate concepts list...
        var list = $('#olConcepts');

        list.empty();

        var checked = $("input[name='keywords']:checked");
        var length = checked.length;

        if (0 < length) {
            //Certain concepts selected...
            for (var i = 0; i < length; ++i) {
                list.append('<li>' + checked[i].value + '</li>');
            }

        }
        else {
            //All concepts selected...
            list.append('All');
        }

    }
    else //hierearchy
    {
        //Uncheck/enable all 'Common' tab checkboxes
        $("input[name='keywords']:checked").attr('checked', false);
        $("input[name='keywords']").prop("disabled", false);

        //return false;

        //Clear and re-populate concepts list...
        var list = $('#olConcepts');

        list.empty();

        var tree = $("#tree").fancytree("getTree");

        //var checked = $("input[name='keywords']:checked");
        //var length = checked.length;
        // var selectedNodes = tree.getSelectedNodes();
        //var length = selectedNodes.length;
        var selKeys = $.map(tree.getSelectedNodes(true), function (node) { //true switch returnes all top level nodes

            return node.title;


        }).join("##");

        if (selKeys.split('##').length > 0) {
            for (var i = 0; i < (selKeys.split('##').length) ; i++) {

                list.append('<li>' + selKeys.split('##')[i] + '</li>');
                //list.append(selKeys[i]);
            }
            //list.append('<li>' + selectedNodes[i].title + '</li>');
        }
    }
}

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
    //to fix browser problems calculating size
    var width = window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth;  
    panelwidth = $('#slider').width();
    //alert(width + ',' + panelwidth)
    //var mapWidth = $(window).width() - panelwidth + "px";
    var mapWidth = width - panelwidth + "px";
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
        //BC - Increase slider panel width...
        width: 225,
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
        //controlUI.style.backgroundColor = 'red';
        controlUI.style.border = '1px';
        controlUI.style.borderRadius = '3px';
        controlUI.style.boxShadow = '0 2px 6px rgba(0,0,0,.3)';
        controlUI.style.cursor = 'pointer';
        controlUI.style.marginBottom = '2px';
        controlUI.style.textAlign = 'center';
        controlUI.title = 'Click to show/hide side panel';
        controlDiv.appendChild(controlUI);
       

        // Set CSS for the control interior
        var controlText = document.createElement('button');
        controlText.style.color = 'rgb(25,25,25)';
        controlText.style.fontFamily = 'Roboto,Arial,sans-serif';
        controlText.style.fontSize = '16px';
        controlText.style.lineHeight = '25px';
        controlText.style.paddingTop = '15px';
        controlText.style.paddingLeft = '3px';
        controlText.style.paddingRight = '3px';
        controlText.style.border = '5px'
        controlText.innerHTML = '< >';
        controlText.id = 'trigger';
        controlUI.appendChild(controlText);

        // Setup the click event listeners: simply set the map to
    // Chicago

        google.maps.event.addDomListener(controlUI, 'click', function () {
            if (sidepanelVisible) {
                slider.slideReveal("hide")
                sidepanelVisible = false
            }
            else {
                slider.slideReveal("show")
                sidepanelVisible = true
            }
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

        //Retain the current place name
        currentPlaceAddress = place.formatted_address;
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
    //resetUserSelection()
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
    var startDate = $("#startDate").val().toString();
    if (checkReg2(startDate)==false) { bootbox.alert("Please validate your From: date"); return}
    var endDate = $("#endDate").val()
    if (checkReg2(endDate)==false) { bootbox.alert("Please validate your To: date"); return }
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
        var allChildrenSelected = false;
        var selKeys = $.map(tree.getSelectedNodes(true), function (node) { //true switch returnes all top level nodes

            return node.title;


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
//http://stackoverflow.com/questions/5465375/javascript-date-regex-dd-mm-yyyy

function checkReg2(date) {
    var date_regex = /^(0[1-9]|1[0-2])\/(0[1-9]|1\d|2\d|3[01])\/(19|20)\d{2}$/;
    if (!(date_regex.test(date))) {
        return false;
    }
}
//upddate map wit new clusters
function updateMap(isNewRequest) {
    
  
    if (clusteredMarkersArray.length == 0 && isNewRequest == false) return;//only map navigation
    var formData = getFormData();
    if (typeof formData == "undefined") return; //error in formdate retrieval

    $("#pageloaddiv").show();

    //get the action-url of the form
    var actionurl = '/home/updateMarkers';
    
    if (clusteredMarkersArray.length == 0) {


        formData.push({ name: "isNewRequest", value: true });
    }
    else {
        formData.push({ name: "isNewRequest", value: false });
    }
   //Clean up
        deleteClusteredMarkersOverlays()
   // }
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
            icon: new google.maps.MarkerImage(clusterMarkerPath + 'm6_single.png', new google.maps.Size(25, 25), null, null, new google.maps.Size(25, 25)),
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

        //BC - disable mouse event listening for now...
        //setMouseEventListeners(map, marker, clusterid);

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

            $('#SeriesModal #myModalLabel').html('List of Timeseries for Selected Marker');

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

        //BC - disable mouse event listening for now...
        //setMouseEventListeners(map, marker, clusterid);

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
            $('#SeriesModal #myModalLabel').html('List of Timeseries for Selected Marker');
                
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
    //$('#example').DataTable();
   
    //var $modal = $('#ajax-modal');
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

//BC - disable mouse event listening for now...
/*
function setMouseEventListeners(map, marker, clusterId) {

    //mouseover - set infowindow content and open...
    google.maps.event.clearListeners(marker, 'mouseover');
    google.maps.event.addListener(marker, 'mouseover', function () {

        infoWindow.setContent('Cluster Id:' + clusterId.toString());
        infoWindow.open(map, marker);
    });

    //mouseout - close infowindow...
    google.maps.event.clearListeners(marker, 'mouseout');
    google.maps.event.addListener(marker, 'mouseout', function () {

        infoWindow.close();
    });

}
*/

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
        "order": [2,'asc'],
         "columns": [
            
            { "data": "Organization" },
            { "data": "ServiceCode", "visible": false},
            { "data": "Title" },
            { "data": "DescriptionUrl", "visible": false },
            { "data": "ServiceUrl", "visible": false },
            { "data": "Checked", "visible": false },
           
            { "data": "Sites", "visible": true },
            { "data": "Variables", "visible": true },
            { "data": "Values", "visible": true },
            { "data": "ServiceBoundingBox", "visible": false},
            { "data": "ServiceID" }
         ],
         //"rowCallback": function( row, data ) {
         //    if ( $.inArray(data.DT_RowId, mySelectedServices) !== -1 ) {
         //        $(row).addClass('selected');
         //    }
         //},
         "createdRow": function ( row, data, index ) {

             var id = $('td', row).eq(5).html();
             var title = $('td', row).eq(1).html();
             var url = 'http://hiscentral.cuahsi.org/pub_network.aspx?n=';
             $('td', row).eq(1).html("<a href='" + url + id + "' target='_Blank'>" + title + " </a>");


         },
         //"scrollX": true,
         initComplete: function () {
             this.fnAdjustColumnSizing();
         }
    })
    //$('a.toggle-vis').on('click', function (e) {
    //    e.preventDefault();

    //    // Get the column API object
    //    var column = table.column($(this).attr('data-column'));

    //    // Toggle the visibility
    //    column.visible(!column.visible());
    //});


    $('#dtServices tbody').on('click', 'tr', function () {
        $(this).toggleClass('selected');
        var id = this.cells[5].innerHTML;
        if ($.inArray(id, mySelectedServices ) == -1) {
            //add
            mySelectedServices.push(id);
        }
        else {            
            //remove
            mySelectedServices = $.grep(mySelectedServices, function (element, index) {
                return element !== id;
            });
        }
        updateServicesList();
    });

    $('#saveServiceSelection').click(function () {
       // alert(myServicesDatatable[0].rows('.selected').data().length + ' row(s) selected');
       
       
       
    });

    $('#cancelServiceSelection').click(function () {
        mySelectedServices.length = 0;

    });
    //return table;
}
//Datatable for Marker
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
        "dom": 'C<"clear">l<"toolbar">frtip',   //Add a custom toolbar - source: https://datatables.net/examples/advanced_init/dom_toolbar.html
         //colVis: {
         //    //restore: "Restore",
         //    //showAll: "Show all",
         //    //showNone: "Show none",
         //    activate: "mouseover",
         //    exclude: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21],
         //    groups: [
         //       //{
         //       //    title: "Main",
         //       //    columns: [ 0, 1,4, 5,6,7,9]
         //       //},
         //       {
         //           title: "Show All Columns",
         //           columns: [ 2, 3, 8, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21]
         //       }
         //    ]
         //},
         "columns": [
            { "data": "Organization", "width": "50px", "visible": true },
            { "data": "ServCode", "sTitle": "Service Name", "visible": true },
            { "data": "ConceptKeyword", "sTitle": "Keyword", "visible": true },
            { "data": "ServURL", "visible": false },
            { "data": "SiteCode", "visible": false },
            { "data": "VariableCode", "visible": false },
              { "data": "VariableName","width": "50px", "sTitle": "Variable Name"},
            { "data": "BeginDate", "sTitle": "Start Date" },
            { "data": "EndDate","sTitle": "End Date" },
            { "data": "ValueCount" },
            { "data": "SiteName", "sTitle": "Site Name" },
            //{ "data": "Latitude", "visible": true },
            //{ "data": "Longitude", "visible": true },
            { "data": "DataType", "visible": true },
            { "data": "ValueType", "visible": true },
            { "data": "SampleMedium", "visible": true },
            { "data": "TimeUnit", "visible": true },
            //{ "data": "GeneralCategory", "visible": false },
            { "data": "TimeSupport", "visible": true },
           
            { "data": "IsRegular", "visible": true },
            //{ "data": "VariableUnits","visible": false },
            //{ "data": "Citation", "visible": false }            
            { "data": "SeriesId" }
        ],
        
         "scrollX": true, //removed to fix column alignment 
         
         "createdRow": function (row, data, index) {

             //If the new row is in top <selectedTimeSeriesMax>, mark the row as selected per check box state...
             if ($('#chkbxSelectAll').prop('checked')) {

                 //Find the position of the new row per the current sort/search order...
                 var table = $('#dtMarkers').DataTable();
                 var position = table.rows()[0].indexOf(index);

                 if (position < selectedTimeSeriesMax) {
                 var jqueryObject = $(row);
                     var className = 'selected';

                     if (!jqueryObject.hasClass(className)) {
                         jqueryObject.addClass(className);
             }
                 }                 
             }

             //if (data[0].replace(/[\$,]/g, '') * 1 > 250000) {

             //var id = $('td', row).eq(14).html();//only visible count
             //var d = $('td', row).eq(4).html();
             //$('td', row).eq(0).append("<a href='/Export/downloadFile/" + id + "' id=" + id + " <span class='glyphicon glyphicon-download-alt'  aria-hidden='true'> </span> </a>");

/* BC - TEST - Do not include download icon or href in any table row... 
              //BC TEST - For IE - can we replace <a> with <span>???
             //$('td', row).eq(0).append("<a href='/Export/downloadFile/" + id + "' id='" + id + "' <span><img  src='/Content/Images/download-icon-25.png' ></span> </a>");
             $('td', row).eq(0).append("<span href='/Export/downloadFile/" + id + "' id='" + id + "' <span><img  src='/Content/Images/download-icon-25.png' ></span> </span>");
             $('td', row).eq(0).click(function (event) {

                 //BC - TEST - Prevent the default action - jquery-filedownload handles the 'GET' request...
                 event.preventDefault();

                 event.stopImmediatePropagation();  //Does this help with IE's open/save dialog?

                 //downloadtimeseries('csv', id); return false;/Content/Images/ajax-loader-green.gif
                 //$('#spinner' + id).removeClass('hidden')
                 $(this).append("<span><img class='spinner' src='/Content/Images/ajax-loader-green.gif' id='" + id  + "'></span>");
                 //$.fileDownload($(this).prop('href'), {
                 //    preparingMessageHtml: "We are preparing your report, please wait...",
                 //    failMessageHtml: "There was a problem generating your report, please try again."
                 //})
                 
                 //BC TEST - For IE - can we replace <a> with <span>???
                 //var hrefProp = $(this).children('a').attr('href');   //href from the <a> element...
                 var hrefProp = $(this).children('span').attr('href');   //href from the <a> element...
                 var imgSelector = 'img[id="' + id + '"]';
                 $.fileDownload(hrefProp)
                     .done(function () {
                         $(imgSelector).addClass('hidden');
                         $(imgSelector).parent().parent().addClass('selected');

                     })
                     .fail(function () {
                         $(imgSelector).addClass('hidden');
                         $(imgSelector).parent().parent().addClass('downloadFail');
                     });

                 return (false);
             });
*/

             //}
         },
        initComplete: function () {

            setfooterFilters('#dtMarkers', [0,1,2,6]);
        
           // oTable.fnAdjustColumnSizing();
        }
        
          

        //"retrieve": true
    });
    //workaround reorder to align headers

    //BC - Test - make each table row selectable by clicking anywhere on the row...
    //Source: https://datatables.net/examples/api/select_row.html
    //Avoid multiple registrations of the same handler...
    $('#dtMarkers tbody').off('click', 'tr', toggleSelected);
    $('#dtMarkers tbody').on('click', 'tr', {'tableId': '#dtMarkers', 'btnId': '#btnZipSelections'},toggleSelected);

    //BC - Test - add a custom toolbar to the table...
    //source: https://datatables.net/examples/advanced_init/dom_toolbar.html
    $("div.toolbar").html('<span style="float: left; margin-left: 1em;"><input type="checkbox" class="ColVis-Button" id="chkbxSelectAll" style="float:left;"/>&nbsp;Select Top ' + 
                          selectedTimeSeriesMax.toString() + '?</span>' +
                          '<input type="button" style="margin-left: 2em; float:left;" class="ColVis-Button btn btn-primary" disabled id="btnZipSelections" value="Zip Selections"/>' +
                          '<span class="clsZipStarted" style="display: none; float:left; margin-left: 2em;"></span>');
    //$("div.toolbar").css('border', '1px solid red');
  
    // BC - Do not respond to data table load event...
    //Add data load event handler...
    //$('#dtMarkers').off('xhr.dt', dataTableLoad);
    //$('#dtMarkers').on('xhr.dt', { 'chkbxId': '#chkbxSelectAll'}, dataTableLoad);

    //Add click handlers...

    //Avoid multiple registrations of the same handler...
    $('#chkbxSelectAll').off('click', selectAll);
    $('#chkbxSelectAll').on('click', {'tableId': '#dtMarkers', 'chkbxId': '#chkbxSelectAll', 'btnId': '#btnZipSelections'}, selectAll);

    //Avoid multiple registrations of the same handler...
    $('#btnZipSelections').off('click', zipSelections);
    $('#btnZipSelections').on('click', { 'tableId': '#dtMarkers', 'chkbxId': '#chkbxSelectAll'}, zipSelections);

    //BC - TEST - Retrieve the colvis button control - assign a click handler for scrollx control...
    //var colvis = new $.fn.DataTable.ColVis(oTable);
    //var button = colvis.button();

    //Avoid multiple registrations of the same handler...
   // $(button).off('click', clovisButtonClick);
   // $(button).on('click', clovisButtonClick);

    
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

        }

//Create 'select'-based filters for the input tableId and columns array
function setfooterFilters(tableId, columnsArray) {

    var api = $(tableId).DataTable();

    api.columns().indexes().flatten().each(function (i) {
        if (-1 !== columnsArray.indexOf(i)) {
            var column = api.column(i);
            var data = column.data();
            var select = $('<select><option value=""></option></select>')
                .appendTo($(column.footer()).empty())
                .on('change', function () {
                    var val = $.fn.dataTable.util.escapeRegex($(this).val());

                    column.search(val ? '^' + val + '$' : '', true, false).draw();
                });

            column.data().unique().sort().each(function (d, j) {
                select.append('<option value="' + d + '">' + d + '</option>')
            });
        }
    });

    $(tableId).dataTable().fnAdjustColumnSizing();
}

function updateServicesList()
{
    //Clear and re-populate services list...
    var list = $('#olServices');
    var length = mySelectedServices.length;
    

    list.empty();

    if (0 < length) {
        //Certain services selected...
        list.append('Selected ' + length.toString() + ' of ' + myServicesDatatable.fnGetData().length + ' .');
    }
    else {
        //All services selected...
        list.append('Selected All of ' + myServicesDatatable.fnGetData().length + '');
    }
}

function toggleSelected(event) {
    $(this).toggleClass('selected');

    //Check state of 'Zip Selections' button...
    enableDisableButton(event.data.tableId, event.data.btnId);
}

function clovisButtonClick(event) {
    var table = $('#dtMarkers');
    var option = false;
    var jsonScrollX = { 'scrollX': option };

    var datatable = $('#dtMarkers').DataTable();
    var colvis = new $.fn.DataTable.ColVis(datatable);
    var colvisbutton = colvis.button();

    //if ($(button).is(':checked')) {
    if ($(colvisbutton).is(':checked')) {
            option = true;
    }

    table.dataTable(jsonScrollX);
}

//Add a new row to the input table...
function newRow($table, cols) {
    var $row = $('<tr/>');
    for (i = 0; i < cols.length; i++) {
        var $col = $('<td/>');
        $col.append(cols[i]);
        $row.append($col);
    }
    $table.append($row);
    return ($row);
}

//Add styles to row for download manager table...
function addRowStylesDM(newrow) {

    //Cell: 0
    var td = newrow.find('td:eq(0)');
    td.addClass('text-center');
    td.css({ 'vertical-align': 'middle', 'height': '2em', 'display': 'none' });

    //Cell: 1
    td = newrow.find('td:eq(1)');
    td.addClass('text-center');
    td.css({ 'vertical-align': 'middle', 'height': '2em', 'width': '15%' });

    //Cell: 2
    td = newrow.find('td:eq(2)');
    td.addClass('text-center');
    td.css({ 'vertical-align': 'middle', 'height': '2em', 'width': '25%' });

    //Cell: 3
    td = newrow.find('td:eq(3)');
    td.addClass('text-center');
    td.css({ 'vertical-align': 'middle', 'height': '2em', 'width': '40%', 'overflow': 'hidden', 'text-overflow': 'ellipsis' });

    //Cell: 4
    td = newrow.find('td:eq(4)');
    td.addClass('text-center');
    td.css({ 'vertical-align': 'middle', 'height': '2em', 'width': '13%' /*, 'margin-left': '-2em' */ });
}

function selectAll(event) {

    //Retrieve all the table's RENDERED <tr> elements whether visible or not, in the current sort/search order...
    //Source: http://datatables.net/reference/api/rows().nodes()
    var table = $(event.data.tableId).DataTable();
    var rows = table.rows({'order': 'current', 'search': 'applied'});    //Retrieve rows per current sort/search order...
    var nodesRendered = rows.nodes();                                    //Retrieve all the rendered nodes for these rows
                                                                         //NOTE: Rendered nodes retrieved in the same order as the rows...
    var jqueryObjects = nodesRendered.to$();    //Convert to jQuery Objects!!
    var className = 'selected';

    //Remove selected class from all rendered rows...
    jqueryObjects.removeClass(className);

    //Apply 'selected' class to the 'top' <selectedTimeSeriesMax> rendered nodes, if indicated 
    if ($(event.data.chkbxId).prop("checked")) {
        var length = nodesRendered.length;

        //For each rendered node...
        for (var i = 0; i < length; ++i) {

            //Determine the position of the associated row in the current sort/search order...
            var position = rows[0].indexOf(nodesRendered[i]._DT_RowIndex)
            
            if (position < selectedTimeSeriesMax) {
                //Row is within the 'top' <selectedTimeSeriesMax> - apply class...
                var jqueryObject = $(nodesRendered[i]);

                if (null !== jqueryObject) {
                    jqueryObject.addClass(className);   //Apply class...
                }
            }
    }
    }

    //Check state of 'Zip Selections' button...
    enableDisableButton(event.data.tableId, event.data.btnId);
}

//Set/reset 'disabled' attribute on referenced button per contents of referenced Data Table...
function enableDisableButton(tableId, btnId) {

    var table = $(tableId).DataTable();
    var selectedRows = table.rows('.selected').data();
    var selectedCount = selectedRows.length;

    if (0 < selectedCount) {
        $(btnId).prop('disabled', false);
    }
    else {
        $(btnId).prop('disabled', true);
    }
}

//Dispaly the referenced label class and then fade...
function displayAndFadeLabel(labelClass) {

    //Display inline... 
    $(labelClass).fadeIn({'duration': 1500});

    setTimeout(function () {
        $(labelClass).fadeOut({ 'duration': 1500 });
    }, 5000);
}

//Enable/disable 'Select All' checkbox per max. selectable row count check...
function dataTableLoad(event, settings, json, xhr) {

    if ((null !== json) && (null !== json.data)) {
        //Successful AJAX query - check total rows received against max. selectable row count...
        var length = json.data.length;
        var propValue = (selectedTimeSeriesMax >= length) ? false : true;

        //$(event.data.chkbxId).prop('disabled', propValue);

        //Remove the tooltip and title attributes...
        $(event.data.chkbxId).removeAttr('data-toggle data-placement title');
        $(event.data.chkbxId).removeClass('disabled');
        //$(event.data.chkbxId).prop('disabled', propValue);

        if (true === propValue) {
            //Checkbox disabled - add disabled class 
            $(event.data.chkbxId).addClass('disabled');

            //Add tooltip explaining why
            $(event.data.chkbxId).attr('data-toggle', 'tooltip');
            $(event.data.chkbxId).attr('data-placement', 'right');
            $(event.data.chkbxId).attr('title', 'The total timeseries rows (' + length.toString() + ') exceed the selectable maximum (' + selectedTimeSeriesMax.toString() + ')');

            //Enable Bootstrap tooltips...
            $('[data-toggle="tooltip"]').tooltip();
        }
    }
}

function zipSelections(event) {

    //Get the next Task Id...
    var taskId = getNextTaskId();
    var txtClass = '.clsZipStarted';

    //Update the text...
    var txt = 'Task: @@taskId@@ started.  To download the archive, please open Download Manager';
    var txts = txt.split('@@taskId@@');

    $(txtClass).text( txts[0] + taskId.toString() + txts[1]);

    //Display the 'Zip processing started... message
    displayAndFadeLabel(txtClass);

    //Create the list of selected time series ids...
    //var table = $('#dtMarkers').DataTable();
    var table = $(event.data.tableId).DataTable();
    var selectedRows = table.rows('.selected').data();

    if ($(event.data.chkbxId).prop("checked")) {
        //User has clicked the 'Select All' check box - ALL rows selected...
        //NOTE: If the DataTable instance has the 'deferRender' option set - not all rows may have been rendered at this point.
        //        Thus one cannot rely on the selectedRows above, since in this case only rendered rows appear in the selectedRows...
        selectedRows = table.rows().data();
    }

    var selectedCount = selectedRows.length;
    var timeSeriesIds = [];

    for (var i = 0; i < selectedCount; ++i) {
        var row = selectedRows[i];
        //var id = $('td', row).eq(0).html();
        var id = row.SeriesId;
        timeSeriesIds.push(id);
    }

    //NOTE- This code gives the count of selected rows...
    //$('#button').click(function () {
    //    alert(table.rows('.selected').data().length + ' row(s) selected');
    //});

    //Create the request object...
    var requestId = randomId.generateId();
    var requestName = 'SelectedArea';

    if (('undefined' !== typeof currentPlaceAddress) && (null !== currentPlaceAddress)) {
        requestName = currentPlaceAddress.replace(/\s*,\s*/g, '-');  //Replace whitespace and commas with '_' 
    }

    var timeSeriesRequest = {
        "RequestName": requestName,
        "RequestId": requestId,
        "TimeSeriesIds": timeSeriesIds
    };

    var timeSeriesRequestString = JSON.stringify(timeSeriesRequest);

    var actionUrl = "/Export/RequestTimeSeries";

    $.ajax({
        url: actionUrl,
        type: 'POST',
        dataType: 'json',
        //timeout: 60000,
        //processData: false,
        cache: false,           //Per IE...
        async: true,
        data: timeSeriesRequestString,
        contentType: 'application/json',
        success: function (data, textStatus, jqXHR) {
            //Retrieve response data
            var response = jQuery.parseJSON(data);

            //alert("Response received: " + response.Status.toString());

            //Add new row to the download manager table
            var cols = [];

            cols.push(response.RequestId.toString());
            cols.push(taskId);
            cols.push(formatStatusMessage(response.Status));
            cols.push(response.BlobUri);

            var button = $("<button class='stopTask btn btn-warning' style='font-size: 1.5vmin'>Stop Processing</button>");

            button.click(function (event) {
                //Hide the button...
                $(event.target).hide();
                //Send request to server to stop task...
                event.preventDefault();
                //var actionUrl = rootDir + 'home/ET';
                var actionUrl = "/Export/EndTask";

                $.ajax({
                        url: actionUrl + '/' + response.RequestId,
                        type: 'GET',
                        contentType: 'application/json',
                        success: function (data, textStatus, jqXHR) {

                        var statusResponse = jQuery.parseJSON(data);

                            //Update status in download manager table...
                            //var tableRow = $("#tblServerTaskCart > td").filter(function () {
                        var tableRow = $("#tblDownloadManager tr td").filter(function () {
                            return $(this).text() === statusResponse.RequestId;
                        }).parent("tr");

                        tableRow.find('td:eq(2)').html(statusResponse.Status);
                        tableRow.find('td:eq(3)').html(statusResponse.BlobUri);
                        tableRow.find('td:eq(5)').html(statusResponse.RequestStatus);

                        //Color table row as 'warning'
                        tableRow.removeClass('info');
                        tableRow.addClass('warning');
                        },
                        error: function (xmlhttprequest, textStatus, message) {

                            var n = 6;
                        n++;

                            alert('Failed to request server to stop task ' +message);
                    }
                    });
            });
            
            cols.push(button);
            cols.push(response.RequestStatus);

            //Add the new row and hide the RequestStatus column...
            var newrow = newRow($("#tblDownloadManager"), cols);
            newrow.find('td:eq(5)').hide();

            //Center the button in the td-3
            //var td3 = newrow.find('td:eq(3)');
            //td3.addClass('text-center');

            addRowStylesDM(newrow);


            //Color row as 'info'
            //newrow.addClass('info');

            //Add a new monitor for the newly created task...
            var intervalId = setInterval(function () {

                //Retrieve status from server
                //var actionUrl = rootDir + 'home/CT';
                var actionUrl = "/Export/CheckTask";

                $.ajax({
                    url: actionUrl + '/' + response.RequestId,
                    type: 'GET',
                    contentType: 'application/json',
                    cache: false,   //So IE does not cache when calling the same URL - source: http://stackoverflow.com/questions/7846707/ie9-jquery-ajax-call-first-time-doing-well-second-time-not
                    success: function (data, textStatus, jqXHR) {

                        var statusResponse = jQuery.parseJSON(data);

                        //Retrieve associated row from Download Manager table...
                        //var tableRow = $("#tblServerTaskCart > td").filter(function () {
                        var tableRow = $("#tblDownloadManager tr td").filter(function () {
                            return $(this).text() === response.RequestId;
                        }).parent("tr");

                        //Update table row...
                        //tableRow.find('td:eq(1)').html(statusResponse.Status);
                        //tableRow.find('td:eq(1)').html(formatStatusMessage(statusResponse.Status));
                        tableRow.find('#statusMessageText').html(statusResponse.Status);
                        tableRow.find('td:eq(3)').html(statusResponse.BlobUri);
                        tableRow.find('td:eq(5)').html(statusResponse.RequestStatus);

                        //Color row as 'info'
                        tableRow.addClass('info');

                        //Check for completed/cancelled task
                        var requestStatus = parseInt(tableRow.find('td:eq(5)').html());

                        if (timeSeriesRequestStatus.Completed === requestStatus ||
                            timeSeriesRequestStatus.CanceledPerClientRequest === requestStatus) {
                            //If task completed - re-assign 'Stop Task' button to 'Download'
                            if (timeSeriesRequestStatus.Completed === requestStatus) {
                                //Success - display the base blob URI only 
                                var baseUri = (statusResponse.BlobUri).split('.zip');
                                baseUri[0] += '.zip';

                                tableRow.find('td:eq(3)').html(baseUri[0]);
                                
                                //Create a download button...
                                var button = tableRow.find('td:eq(4)');

                                button = $("<button class='zipBlobDownload btn btn-success' style='font-size: 1.5vmin'>Download Archive</button>");

                                button.on('click', function (event) {
                                    var blobUri = tableRow.find('td:eq(3)').html();
                                    location.href = blobUri;

                                    //Fade row and remove from table...
                                    tableRow.fadeTo(1500, 0.5, function() {
                                            $(this).remove();
                                    });

                                    event.stopPropagation();
                                });

                                tableRow.find('td:eq(4)').html(button);

                                //Change the glyphicon and stop the animation
                                var glyphiconSpan = tableRow.find('#glyphiconSpan');

                                glyphiconSpan.removeClass('glyphicon-refresh spin');
                                glyphiconSpan.addClass('glyphicon-thumbs-up');

                                tableRow.find('#statusMessageText').html(statusResponse.Status);

                                tableRow.addClass('success');   //Color row as 'successful'

                                if ($("#chkbxAutoDownload").prop("checked")) {
                                        //Autodownload checkbox checked - click the newly created button...
                                    button.click();
                                }
                            }
                            else {
                                //Task canceled - color row as 'danger'
                                tableRow.addClass('danger');
                                
                                //Fade row and remove from table...
                                tableRow.fadeTo(1500, 0.5, function() {
                                    $(this).remove();
                                });
                            }
                            
                            //Clear the interval
                            clearInterval(intervalId);
                            return;
                        }
                    },
                    error: function (xmlhttprequest, textStatus, message) {

                        var n = 6;
                        n++;

                        alert('Failed to retrieve task status ' + message);
                    }
                });

            }, 1000);

        },
        error: function (xmlhttprequest, textstatus, message) {
            var n = 6;

            n++;
            alert('Failed request time series: ' + message);
        }
    });
}

//Format the html for the status message as follows - all elements inline:  <h3> - glyphicon spinner </h3> <span> status message text </span> 
function formatStatusMessage(statusText) {
    
    var formattedMessage = '<h3 class="text-center" style="display: inline; margin: 0em 0em 0em 0em;">' + 
                           '<span id="glyphiconSpan" class="glyphicon glyphicon-refresh spin" style="color: #32cd32;"></span></h3>' + //color is CSS LimeGreen
                           '<div id="statusMessageText" class="text-center" style="display:inline-block; margin: 0em 0em 0em 1em; vertical-align: top;">' +
                           statusText +
                           '</div>';
    return ( formattedMessage)
}

//Return the next task Id in format: <++taskCount>@hh:mm:ss
function getNextTaskId() {
    var date = new Date();

    return ((++taskCount).toString() + '@' + date.getHours().toString() + ':' + date.getMinutes().toString() + ':' + date.getSeconds().toString()); 
}

function setUpTimeseriesDatatable() {
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

    //Set page title...
    $('#dataview #myModalLabel').html('List of Timeseries for Selected Area');


    //var dataSet = getDetailsForMarker(clusterid)
    var actionUrl = "/home/getTimeseries"
    // $('#example').DataTable().clear()
    // $('#demo').html( '<table cellpadding="0" cellspacing="0" border="0" class="display" id="example"></table>' );
    var oTable = $('#dtTimeseries').dataTable({
        "ajax": actionUrl,
        "dom": 'C<"clear">l<"toolbarTS">frtip',   //Add a custom toolbar - source: https://datatables.net/examples/advanced_init/dom_toolbar.html
        "deferRender": true,        
        "columns": [
            { "data": "Organization", "width": "50px", "visible": true },
            { "data": "ServCode", "sTitle": "Service Name", "visible": true },
            { "data": "ConceptKeyword", "sTitle": "Keyword", "visible": true },
            { "data": "ServURL", "visible": false },
            { "data": "SiteCode", "visible": false },
            { "data": "VariableCode", "visible": false },
              { "data": "VariableName", "width": "50px", "sTitle": "Variable Name" },
            { "data": "BeginDate", "sTitle": "Start Date" },
            { "data": "EndDate", "sTitle": "End Date" },
            { "data": "ValueCount" },
            { "data": "SiteName", "sTitle": "Site Name" },
            //{ "data": "Latitude", "visible": true },
            //{ "data": "Longitude", "visible": true },
            { "data": "DataType", "visible": true },
            { "data": "ValueType", "visible": true },
            { "data": "SampleMedium", "visible": true },
            { "data": "TimeUnit", "visible": true },
            //{ "data": "GeneralCategory", "visible": false },
            { "data": "TimeSupport", "visible": true },

            { "data": "IsRegular", "visible": true },
            //{ "data": "VariableUnits","visible": false },
            //{ "data": "Citation", "visible": false }            
            { "data": "SeriesId" }
           ],
        "scrollX": true, //removed to fix column alignment 
        "createdRow": function (row, data, index) {

            //If the new row is in top <selectedTimeSeriesMax>, mark the row as selected per check box state...
           if ($('#chkbxSelectAllTS').prop('checked')) {

               //Find the position of the new row per the current sort/search order...
               var table = $('#dtTimeseries').DataTable();
               var position = table.rows()[0].indexOf(index);

               if (position < selectedTimeSeriesMax) {
                     var jqueryObject = $(row);
                     var className = 'selected';

                     if (!jqueryObject.hasClass(className)) {
                         jqueryObject.addClass(className);
                    }
                }
           }

            //if (data[0].replace(/[\$,]/g, '') * 1 > 250000) {

            var id = $('td', row).eq(0).html();
            //var d = $('td', row).eq(4).html();

            /* BC - TEST - Do not include download icon or href in any table row...
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
*/
        },
        initComplete: function () {

            setfooterFilters('#dtTimeseries', [0,1, 2, 6]);
        }

    });
      
    //BC - Test - make each table row selectable by clicking anywhere on the row...
    //Source: https://datatables.net/examples/api/select_row.html
    //Avoid multiple registrations of the same handler...
    $('#dtTimeseries tbody').off('click', 'tr', toggleSelected);
    $('#dtTimeseries tbody').on('click', 'tr', { 'tableId': '#dtTimeseries', 'btnId': '#btnZipSelectionsTS' }, toggleSelected);

    //BC - Test - add a custom toolbar to the table...
    //source: https://datatables.net/examples/advanced_init/dom_toolbar.html
    $("div.toolbarTS").html('<span style="float: left; margin-left: 1em;"><input type="checkbox" class="ColVis-Button" id="chkbxSelectAllTS" style="float:left;"/>&nbsp;Select Top ' +
                            selectedTimeSeriesMax.toString() + '?</span>' +
                            '<input type="button" style="margin-left: 2em; float:left;" class="ColVis-Button btn btn-primary" disabled id="btnZipSelectionsTS" value="Zip Selections"/>' +
                            '<span class="clsZipStarted" style="display: none; float:left; margin-left: 2em;"></span>');

    // BC - Do not respond to data table load event...
    //Add data load event handler...
    //$('#dtTimeseries').off('xhr.dt', dataTableLoad);
    //$('#dtTimeseries').on('xhr.dt', { 'chkbxId': '#chkbxSelectAllTS' }, dataTableLoad);

    //Add click handlers...

    //Avoid multiple registrations of the same handler...
    $('#chkbxSelectAllTS').off('click', selectAll);
    $('#chkbxSelectAllTS').on('click', { 'tableId': '#dtTimeseries', 'chkbxId': '#chkbxSelectAllTS', 'btnId': '#btnZipSelectionsTS'}, selectAll);

    //Avoid multiple registrations of the same handler...
    $('#btnZipSelectionsTS').off('click', zipSelections);
    $('#btnZipSelectionsTS').on('click', { 'tableId': '#dtTimeseries', 'chkbxId': '#chkbxSelectAllTS'}, zipSelections);

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

    if (typeof (xmlhttprequest.responseText) != 'undefined')
    {
        
        var msg = JSON.parse(xmlhttprequest.responseText)
        bootbox.alert(msg.Message);
    }
    else {
        bootbox.alert("<H4>An error accored: " + message + ". Please limit search area or Keywords. Please contact Support if the problem persists.</H4>");

    }
    //clean up
    $("#pageloaddiv").hide();
    resetMap();
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


