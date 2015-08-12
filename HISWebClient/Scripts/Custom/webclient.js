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
var mySavedServices = [];
var bObservedValuesOnly = false;
var mySelectedTimeSeries = [];
var sessionGuid;
var randomId;
var currentPlaceName = '';
var currentMarkerPlaceName = '';
var timeSeriesRequestStatus;
var slider;
var sidepanelVisible = false;

var selectedTimeSeriesMax = 25;

var conceptsType = '';

var selectedRowCounts = {
    'dtMarkers': {'targetId': 'btnZipSelections', 'count': 0},
    'dtTimeseries': { 'targetId': 'btnZipSelectionsTS', 'count': 0 }
};

//BC - 19-Jun-2015 - Disable concept counting - possible later use...
//var selectedConceptsMax = 4;

//lisy of services that only have 
var ArrayOfNonObservedServices = ["84","187","189","226","262","267","274"]

var taskCount = 0;

$(document).ready(function () {

    $("#pageloaddiv").hide();
    initialize();

    //Periodically check selected time series...
    setInterval(function () {
        //console.log('Checking selected time series...');
        var firstPart = 'Process '
        var secondPart = ['Selection(s)', 'Selection', ' Selections'];        //For each tableId in selected row counts...

        for (var tableId in selectedRowCounts) {
            if ($('#' + tableId).is(":visible")) {
                //table visible - evaluate count
                var count = selectedRowCounts[tableId].count;
                var targetId = selectedRowCounts[tableId].targetId;

                if (0 < count) {
                    //Selections found - update targetId text...
                    $('#' + targetId).val(firstPart + (1 < count ? count + secondPart[2] : secondPart[1]));
                } else {
                    //Nothing selected - reset targetId text...                    
                    $('#' + targetId).val(firstPart + secondPart[0]);
                }
            }
        }
    }, 250);

});

function initialize() {

    var myCenter = new google.maps.LatLng(39, -92); //us
    //var myCenter = new google.maps.LatLng(42.3, -71);//boston
    // var myCenter = new google.maps.LatLng(41.7, -111.9);//Salt Lake

    //NOTE: Set the place name to match the initial latitude and longitude 
    currentPlaceName = 'United States';

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
            opened: true
        },
        zoomControl: true,
        panControl: false,
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

    //get reference to map sizing of window happens later to prevent gap   
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
            //
        }
        //$("#MapAreaControl").html(getMapAreaSize());
    });
    google.maps.event.addListener(map, 'dragend', function () {
        if ((clusteredMarkersArray.length > 0)) {
            updateMap(false)

        }
        //$("#MapAreaControl").html(getMapAreaSize());
    });

    google.maps.event.addListener(map, 'zoom_changed', function () {
        if ((clusteredMarkersArray.length > 0)) {
            updateMap(false)



        }

    });
    //added to load size on startup
    google.maps.event.addListener(map, 'idle', function () {
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
        if (sidepanelVisible) {
            slider.slideReveal("show")
        }
    });


    //initialize datepicker for the nonmodal date range...
    //NOTE: The datepicker can be initialized only once!!  
    //$('.non-modal-inputs').datepicker({ 'inputs': $('.non-modal-date-range').toArray(), 'todayHighlight': true });

    //Set modal datepicker options...
    //$('.modal-inputs').datepicker({ 'inputs': $('.modal-date-range').toArray(), 'todayHighlight': true });

    //Assign click handlers to panel date inputs
    $('#startDate').on('click', function () { $('#btnDateRange').click() });
    $('#endDate').on('click', function () { $('#btnDateRange').click() });


    //Initialize multiple datepicker instances on different input ids...
    $('#startDateModal').datepicker({ 'todayHighlight': true });
    $('#endDateModal').datepicker({ 'todayHighlight': true });



    //Set start and end of date range...
    var initDate = new Date();

    initializeDate(initDate, 'endDate');

    //Calculate start date as one year earlier...
    //NOTE: For an end date === 29-Feb-<YYYY>, the resulting start date === 01-Mar-<YYYY - 1>
    //      Since setFullYear() returns a number, not a Date object, the setFullYear(...) expression cannot passed to initializeDate(...) directly!!
    initDate.setFullYear(initDate.getFullYear() - 1);
    initializeDate(initDate, 'startDate');

    //Assign date validation handlers...
    //$('#startDate').on('blur', { 'groupId': 'grpStartDate', 'errorLabelId': 'lblStartDateError' }, validateDateString);
    //$('#endDate').on('blur', { 'groupId': 'grpEndDate', 'errorLabelId': 'lblEndDateError' }, validateDateString);

    $('#startDateModal').on('blur', { 'groupId': 'grpStartDateModal', 'errorLabelId': 'lblStartDateErrorModal' }, validateDateString);
    $('#endDateModal').on('blur', { 'groupId': 'grpEndDateModal', 'errorLabelId': 'lblEndDateErrorModal' }, validateDateString);


    //Assign future date checks...
    $('#startDate, #endDate').on('blur', { 'groupId': 'grpStartDate', 'errorLabelId': 'lblStartDateError', 'inputIdStartDate': 'startDate', 'inputIdEndDate': 'endDate' }, compareFromDateAndToDate);

    $('#startDateModal, #endDateModal').on('blur', { 'groupId': 'grpStartDateModal', 'errorLabelId': 'lblStartDateErrorModal', 'inputIdStartDate': 'startDateModal', 'inputIdEndDate': 'endDateModal' }, compareFromDateAndToDate);

    //Button click handler for Select Date Range...
    $('#btnDateRange').on('click', function (event) {
        //Assign current start and end date values to their modal counterparts...
        var startDate = $('#startDate').val();
        var endDate = $('#endDate').val();

        $('#startDateModal').val(startDate);
        $('#endDateModal').val(endDate);

        //Assign current start and end date values to the associated datepicker instances
        $('#startDateModal').datepicker('setDate', startDate);
        $('#endDateModal').datepicker('setDate', endDate);
    });

    //Button click handler for Date Range Modal Save 
    $('#btnDateRangeModalSave').on('click', function (event) {

        //Simulate 'blurs' to start and end date inputs to invoke date validation checking...
        $('#startDateModal').blur();
        $('#endDateModal').blur();

        //Check for date validation errors...
        if ($('#startDateModal').hasClass('has-error') || $('#endDateModal').hasClass('has-error')) {
            return false;   //Validation error(s) - DO NOT close the dialog...
        }

        //Assign current modal start and end date values to their main page counterparts...

        //var startDateModal = $("#startDateModal").val()
        //if (checkReg2(startDateModal) == false) { bootbox.alert("Please validate your From: date"); return }
        $('#startDate').val($('#startDateModal').val());

        //var endDateModal = $("#endDateModal").val()
        //if (checkReg2(endDateModal)==false) { bootbox.alert("Please validate your To: date"); return }
        $('#endDate').val($('#endDateModal').val());
    });

    //initialize show/hide for search box
    $('.expander').on('click', function () {
        $('#selectors').slideToggle();
    });


    //allocate a RandomId instance...
    randomId = new RandomId({
        'iterationCount': 10,
        'characterSets': ['alpha', 'numeric']
    });

    //Allocate a TimeSeriesRequestStatus instance...
    timeSeriesRequestStatus = (new TimeSeriesRequestStatus()).getEnum();

    var steInt = timeSeriesRequestStatus.StopTaskError;

    var steObj = timeSeriesRequestStatus.properties[steInt];

    var desc  = steObj.description;

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

        //updateKeywordList("Common");       
        updateKeywordList();

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
        else {
            for (var i = 0; i < rows.length; i++) {
                // Get HTML of 3rd column (for example)

                //check if value in array of non observed services



                //mark with select class
                $(rows[i]).removeClass('selected');

            }
        }

        //Enable 'Clear Selections' button, if indicated...
        if (0 < mySelectedServices.length) {
            $('#btnClearSelectionsDS').prop('disabled', false);
        }
        else {
            $('#btnClearSelectionsDS').prop('disabled', true);
        }

    });

    $("#Search").submit(function (e) {

        resetMap()
        //prevent Default functionality
        e.preventDefault();
        e.stopImmediatePropagation();
        //var formData = getFormData();
        var path = [];
        path = GetPathForBounds(map.getBounds())
        var area = GetAreaInSquareKilometers(path)

        //BCC - 10-Jul-2015 
        // Since keyword selections on 'Common' and 'Full' tabs are kept in sync at all times, 
        //  retrieve selected keys from 'Full' tab...
        var selectedKeys = [];
        var tree = $("#tree").fancytree("getTree");

        selectedKeys = $.map(tree.getSelectedNodes(true), function (node) { //true switch returns all top level nodes
            return node.title;
        });

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
        var areaLeft = google.maps.geometry.spherical.computeArea(areaRect.getPath());

    });

    //BCC - 02-Jul-2015 - External QA Issue #48 
    $("#btnSetDefaults").on('click', function () {
        //Remove all map markers, if any...
        resetMap();

        //reset search parameters...
        resetSearchParameters();
    });

    //click event for tab
    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
        if (e.target.id == "tableTab") {
            //Hide the zendesk iframe...
            $('#launcher').hide();

            //Reset the current marker place name...
            currentMarkerPlaceName = '';

            setUpTimeseriesDatatable();
            var table = $('#dtTimeseries').DataTable();

            //BCC - 29-Jun-2015 - QA Issue # 26 - Data tab: filters under the timeseries table have no titles
            //Assign a handler to the DataTable 'draw.dt' event
            table.off('draw.dt', addFilterPlaceholders);
            table.on('draw.dt', { 'tableId': 'dtTimeseries' }, addFilterPlaceholders);

            //BCC - 09-Jul-2015 -  - QA Issue #25 - Data tab: table column titles stay in wrong order after sorting table
            //Remove/add column adjustment event handler...
            $('#dtTimeSeriesModal').off('shown.bs.modal', adjustColumns);
            $('#dtTimeSeriesModal').on('shown.bs.modal', {
                'tableId': 'dtTimeSeries', 'containerId': 'dtTimeSeriesModal'
            }, adjustColumns);

            table.order([0, 'asc']).draw();
            //hide sidebar
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

    //BCC - 26-Jun-2015 - QA Issue #20 - Select Keywords: selected keywords saved even popup were closed without user click Save
    //Click handler for 'Select Keyword(s)' button
    $('#btnSelectKeywords').on('click', clickSelectKeywords);


    $('#btnSelectDataServices').on('click', clickSelectDataServices);

    //Click handler for 'Common' keywords checkboxes
    $('input[name="keywords"]').on('click', clickCommonKeyword);

    //Shown and hidden handlers for Select Data Services...
    $('#SelectServicesModal').on('shown.bs.modal', shownSelectDataServices);
    $('#SelectServicesModal').on('hidden.bs.modal', hiddenSelectDataServices);
}

//Shown handler for Select Data Services...
function shownSelectDataServices() {
    $('#launcher').hide();
}

//Hidden handler for Select Data Services...
//NOTE: Since the 'olServices' list maintains a count of the selected data services, 
//      the mySavedServices array maintains the ids of those data services selected by
//      the user.
//
//      Since the 'Select Data Services' dialog is modal, we can save processing time by 
//      updating the mySaveServices array whenever the user closes the dialog:
//       - clicks the 'close' button
//       - clicks the 'x' in the upper right corner 
//       - presses the ESC key
//
function hiddenSelectDataServices() {
    $('#launcher').show();

    //Capture any changes in user selections...
    //Rebuild the saved services array...
    mySavedServices.length = 0;

    var rows = $("#dtServices").dataTable().fnGetNodes();
    var length = rows.length;

    for (var i = 0; i < length; ++i) {
        var row = $(rows[i]);

        if (row.hasClass('selected')) {
            mySavedServices.push(row.find('td:eq(5)').html());
        }
    }

    //Update the services list from the newely rebuilt saved services array...
    updateServicesList();

    //Retain the state of the 'Select all non-gridded...' checkbox
    bObservedValuesOnly = $('#checkOnlyObservedValues').prop('checked');
}

//Return all search parameters to initial state
function resetSearchParameters() {
    //Reset 'From' and 'To' Dates...
    var initDate = new Date();

    initializeDate(initDate, 'endDate');
    initDate.setFullYear(initDate.getFullYear() - 1);
    initializeDate(initDate, 'startDate');

    //Reset Keyword(s)...
    initializeKeywordList('Common');
    initializeKeywordList('Full');

    //Reset Data Service(s)...
    initializeDataServices();
}

//Initialize the input id to the current date
function initializeDate(date, inputId) {

    $('#' + inputId).val(('0' + (date.getMonth() + 1).toString().slice(-2)) + '/' + ('0' + date.getDate()).toString().slice(-2) + '/' + (date.getFullYear()).toString());
}

//Initialize the keyword list per the input list type
function initializeKeywordList(type) {

    if ('Common' === type) {
        //'Most Common' keywords...
        $("input[name='keywords']:checked").attr('checked', false);
        $("input[name='keywords']").prop("disabled", false);
    }
    else {
        if ('Full' === type) {
            //'Full' keywords...
            $("#tree").fancytree("getTree").visit(function (node) {
                node.setSelected(false);
                node.unselectable = false;
                node.hideCheckbox = false;
            });
        }
    }

    //Clear the concepts list
    var list = $('#olConcepts');

    list.empty();
    list.append('<li>All</li>');
}

//Initialize the data services table
function initializeDataServices() {

    //Uncheck gridded data services checkbox
    $('#checkOnlyObservedValues').attr('checked', false);

    //Unselect all table rows
    if ($.fn.DataTable.isDataTable('#dtServices')) {
        //Retrieve all the table's RENDERED <tr> elements whether visible or not, in the current sort/search order...
        //Source: http://datatables.net/reference/api/rows().nodes()
        var table = $('#dtServices').DataTable();
        var rows = table.rows({ 'order': 'current', 'search': 'applied' });    //Retrieve rows per current sort/search order...
        var nodesRendered = rows.nodes();                                    //Retrieve all the rendered nodes for these rows
        //NOTE: Rendered nodes retrieved in the same order as the rows...
        var jqueryObjects = nodesRendered.to$();    //Convert to jQuery Objects!!
        var className = 'selected';

        //Remove selected class from all rendered rows...
        jqueryObjects.removeClass(className);
    }

    //Clear the services list/reset global array
    mySelectedServices =[];
    mySavedServices = [];
    list = $('#olServices');

    list.empty();
    list.append('<li>All</li>');
}

    
//Validate the input date string - return true: valid, false otherwise
function validateDateString(event) {

    var jqueryObj = $(this);
    var dateString = jqueryObj.val();
    var jqueryGrpObj = $(('#' + event.data.groupId));
    var jqueryLabelObj = $(('#' + event.data.errorLabelId));

    if (! isNaN(Date.parse(dateString))) {
        //Date string valid - remove error class, hide error message
        jqueryLabelObj.text('');
        jqueryLabelObj.hide();
        jqueryGrpObj.removeClass('has-error');
        jqueryObj.removeClass('has-error');
    }
    else {
        //Date string invalid - set error class, show error message
        jqueryLabelObj.text('Please enter a valid date...');
        jqueryLabelObj.show();
        jqueryGrpObj.addClass('has-error');
        jqueryObj.addClass('has-error');
        //jqueryObj.focus();    //Allow change of focus for bootstrap datepicker!!
    }
}

//Validate the 'from' date string is earlier than or equal to the 'to' date
function compareFromDateAndToDate(event) {

    //Retrieve 'from' and 'to' date strings...
    var jqueryStartDate = $(('#' + event.data.inputIdStartDate));
    var jqueryEndDate = $(('#' + event.data.inputIdEndDate));

    var dateStringStart = jqueryStartDate.val();
    var dateStringEnd = jqueryEndDate.val();

    var jqueryGrpObj = $(('#' + event.data.groupId));
    var jqueryLabelObj = $(('#' + event.data.errorLabelId));


    //Parse both date strings...
    var msStart = Date.parse(dateStringStart);
    var msEnd = Date.parse(dateStringEnd);

    if ((!isNaN(msStart)) && (!isNaN(msEnd))) {
        //Date strings represent valid dates - compare 

        if (msStart <= msEnd) {
            //Success - remove error class, hide error message
            jqueryLabelObj.text('');
            jqueryLabelObj.hide();
            jqueryGrpObj.removeClass('has-error');
            jqueryStartDate.removeClass('has-error');
        }
        else {
            //Failure - set error class, show error message
            jqueryLabelObj.text("Please enter a 'From' date earlier than or equal to the 'To' date...");
            jqueryLabelObj.show();
            jqueryGrpObj.addClass('has-error');
            jqueryStartDate.addClass('has-error');
        }
    }

    //If either date string is invalid - take no action
    return;
}


//BCC - 26-Jun-2015 - Various fixes related to QA Issue #15 - Warning messages: typos in warning message when selecting 2+ keywords and area is 25sq km+
function validateQueryParameters(area, selectedKeys) {
    //validate inputs

    area *= 1;  //Convert to numeric value...

    var max = 1000000;
    if (area > max) {
        
        bootbox.alert("<h4>The selected area (" + area.toLocaleString() + " sq km) is too large to search. <br> Please limit search area to less than " + max.toLocaleString() + " sq km and/or reduce search keywords.</h4>");
        return false;
    }

    max = 250000;
    if (area > max && selectedKeys.length == 0) {
        bootbox.alert("<h4>The selected area (" + area.toLocaleString() + " sq km) is too large to search for <strong>All</strong> keywords. <br> Please limit search area to less than " + max.toLocaleString() + " sq km and/or limit keywords.</h4>");
        return false;
    }
    if (area > max && selectedKeys.length == 1) {
        bootbox.confirm("<h4>Searching the selected area (" + area.toLocaleString() + " sq km) can take a long time. Do you want to continue?</h4>", function (bContinue) {

                if (bContinue) {
                    updateMap(true);
                }
            });
    }
    else {
            if (area > max && selectedKeys.length > 1) {
                bootbox.alert("<h4>For the selected area (" + area.toLocaleString() + " sq km), you selected more than one keyword. <br> Please reduce area or select only one keyword.</h4>");
                return false;
        }
            if (area < max && selectedKeys.length > 1) {
                bootbox.confirm("<h4>For the selected area (" + area.toLocaleString() + " sq km), you selected several keywords. This search can take a long time and might timeout. Do you want to continue?</h4>", function (bContinue) {

                    if (bContinue) {
                        updateMap(true);
                    }
            });
        }
        else
        {
            updateMap(true)
            }     
        }

    return true;
}

//Fancy tree click handler...
function keywordClickHandler(event, data) {

    var node = data.node;

    if (( 'undefined' !== typeof node) && (null !== node)) {

        if (node.isSelected()) {
            console.log( node.title + ' is selected!!');
        }
        else {
            console.log(node.title + ' is NOT selected!!');
        }

        if (node.isActive()) {
            console.log(node.title + ' is active!!');
        }
        else {
            console.log(node.title + ' is NOT active!!');
        }

    }
}

//Fancy tree activate handler...
function keywordActivateHandler(event, data) {

    var node = data.node;

    if (('undefined' !== typeof node) && (null !== node)) {

        if (node.isSelected()) {
            console.log(node.title + ' is selected!!');
        }
        else {
            console.log(node.title + ' is NOT selected!!');
        }

        if (node.isActive()) {
            console.log(node.title + ' is active!!');
        }
        else {
            console.log(node.title + ' is NOT active!!');
        }

    }
}

//Fancy tree click handler...
function keywordSelectHandler(event, data) {

    var node = data.node;

    //if (('undefined' !== typeof node) && (null !== node)) {

    //    console.log('Node: ' + node.title + ' selected?  ' + node.isSelected() );
    //    //console.log('Node: ' + node.title + ' active?  ' + node.isActive());
    //}

    //Scan the checkboxes on the 'Common' tab for a matching value...
    $("input[name='keywords']").each(function (index, element) {
        var checkbox = $(this);
        if( node.title.toLowerCase() === checkbox.prop('value').toLowerCase() && node.isSelected() !== checkbox.is(':checked')) {
                //Matching value found but non-matching states, update checkbox state
                checkbox.prop('checked', node.isSelected());
        }
    });
}

//Update the keyword list from the keyword tab entries
//NOTE: Since the two tabs, 'Common' and 'Full', are kept in sync at all times, update the keyword list from the 'Full' tab...
function updateKeywordList() {

    //Clear and re-populate concepts list...
    var list = $('#olConcepts');

    list.empty();

    var tree = $("#tree").fancytree("getTree");

    var titleCount = 0;
    var selKeys = $.map(tree.getSelectedNodes(true), function (node) { //true switch returns all top level nodes

            ++titleCount;
            return node.title;

        }).join("##");

     if ((0 < titleCount) && (selKeys.split('##').length > 0)) {
            for (var i = 0; i < (selKeys.split('##').length) ; i++) {
            list.append('<li>' + selKeys.split('##')[i] + '</li>');
        }
     }
     else {
        //'No' concepts selected means 'All' concepts selected...
        list.append('<li>All</li>');
     }
/*
    if (type === "Common") {

        //Retain the current concepts type
        conceptsType = type;

        //Unset/enable all 'Full' tab entries...
        initializeKeywordList('Full');

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
            list.append('<li>All</li>');
        }

    }
    else //hierarchy
    {
        //Retain the current concepts type
        conceptsType = 'Hierarchy';

        //Uncheck/enable all 'Common' tab checkboxes
        initializeKeywordList('Common');

        //Clear and re-populate concepts list...
        var list = $('#olConcepts');

        list.empty();

        var tree = $("#tree").fancytree("getTree");

        //var checked = $("input[name='keywords']:checked");
        //var length = checked.length;
        // var selectedNodes = tree.getSelectedNodes();
        //var length = selectedNodes.length;
        var titleCount = 0;
        var selKeys = $.map(tree.getSelectedNodes(true), function (node) { //true switch returns all top level nodes

            ++titleCount;
            return node.title;

        }).join("##");

        if ((0 < titleCount) && (selKeys.split('##').length > 0)) {
            for (var i = 0; i < (selKeys.split('##').length) ; i++) {

                list.append('<li>' + selKeys.split('##')[i] + '</li>');
                //list.append(selKeys[i]);
            }
            //list.append('<li>' + selectedNodes[i].title + '</li>');
        }
        else {
            //All concepts selected...
            list.append('<li>All</li>');
        }
    }
*/
}


//BCC - 26-Jun-2015 - QA Issue #20 - Select Keywords: selected keywords saved even popup were closed without user click Save
//Click handler for 'Select Keyword(s)' button
function clickSelectKeywords(event) {

    //Clear selections from 'Most Common' and 'Full List' tabs...

    //'Most Common' tab checkboxes
    $("input[name='keywords']:checked").attr('checked', false);

    //'Full List' tab nodes
    $("#tree").fancytree("getTree").visit(function (node) {
        node.setSelected(false);
    });

    //Assume keywords data loaded at page initialization - populate tabs, show the dialog...
    populateKeywordTabs();
    $('#SelectVariableModal').modal('show');

}

//Load fancytree nodes, if indicated.  Conditionally display modal 'loading keywords' dialog until fancytree is completely loaded 
//NOTE: While this implementation works OK, it does rely on the called data URL to detemine the 
//      type of keyword data recieved: Physical, Chemical or Biological.  
//      If re-factoring is required later, please refer to the following article for a better
//      designed approach:  Roel van Lisdonk - How to pass extra / additional parameters to the deferred.then() function in jQuery, $q (AngularJS) or Q. 
//      http://www.roelvanlisdonk.nl/?p=3952 - 
function loadKeywordsIntoTree( pShowUI, pbtnKeyword, pbtnText, pglyphiconSpan ) {

    //Validate/initialize input parameters
    var showUI = false;     //Default
    var btnKeyword = null;
    var btnText = null;
    var glyphiconSpan = null;
    
    if (('undefined' !== typeof pShowUI) && ('boolean' === typeof pShowUI)) {
        showUI = pShowUI;
    }

    if (('undefined' !== typeof pbtnKeyword) && (null !== pbtnKeyword)) {
        btnKeyword = '#' + pbtnKeyword; 
    }

    if (('undefined' !== typeof pbtnText) && (null !== pbtnText)) {
        btnText = pbtnText;
    }

    if (('undefined' !== typeof pglyphiconSpan) && (null !== pglyphiconSpan)) {
        glyphiconSpan = '#' + pglyphiconSpan;
    }

    var tree = $("#tree").fancytree("getTree");
    var rootNode = tree.rootNode;
    if (('undefined' !== typeof rootNode) && (null !== rootNode)) {

        var children = rootNode.children;
        if (('undefined' !== typeof children) && (null !== children)) {

            //Disable the keyword button...
            if (null !== btnKeyword) {
                $(btnKeyword).prop('disabled', true);
            }

            //For each child node...
            var bShowModal = true;  //Assume keyword loading dialog will be shown...

            var length = children.length;
            var callsToGo = 0;

            for (var i = 0; i < length; ++i) {

                if (!children[i].isLoaded()) {
                    ++callsToGo;
                    if (bShowModal) {
                        bShowModal = false;
                        if (showUI) {
                            $('#keywordModal').modal('show');   //Show the keyword loading dialog
                        }
                    }

                    children[i].load().done(function (data, textStatus, jqXHR) {
                        //alert('Child node loaded!!');

                        if (('undefined' !== typeof data.testData) && (null !== data.testData)) {
                            console.log('testData received: ' +data.testData);
                    }

                        //Determine the keyword category - Physical, Chemical or Biological from the url...
                        var url = this.url;
                        var urlComponents = url.split('/');
                        var keywordCategory = urlComponents[urlComponents.length -1];

                        var glyphiconSpan = $('#glyphiconSpan' +keywordCategory);
                        var jqueryText = $('#text' +keywordCategory);

                        glyphiconSpan.removeClass('glyphicon-refresh spin');
                        glyphiconSpan.addClass('glyphicon-thumbs-up');

                        var text = jqueryText.html();
                        jqueryText.html(text + ' Completed!!');

                        if (0 >= --callsToGo) {
                            setTimeout(function () {
                                //All load calls complete - close the loading dialog...
                                $('#keywordModal').modal('hide');

                                //Populate keyword tabs
                                populateKeywordTabs();

                                //Open keywords dialog...
                                if (showUI) {
                                    $('#SelectVariableModal').modal('show');
                                }

                                //Enable the keyword button...
                                if (null !== btnKeyword) {
                                    $(btnKeyword).prop('disabled', false);
                                }

                                //Hide the glyphiconSpan...
                                if (null !== glyphiconSpan) {
                                    $(glyphiconSpan).hide();
                                }

                                //Set the keyword button text...
                                if ((null !== btnKeyword) && (null !== btnText)) {
                                    $(btnKeyword).text(btnText);
                                }

                            }, 1000);
                        }
                    }).fail(function (data, textStatus, jqXHR) {
                        //For now - just display an alert...
                        bootbox.alert('Keyword retrieval error - please try again...');
                    });
                }
            }

            if (bShowModal) {
                //Keywords data previously loaded - populate tabs, show the dialog...
                populateKeywordTabs();

                if (showUI) {
                    $('#SelectVariableModal').modal('show');
                }

                //Enable the keyword button...
                if (null !== btnKeyword) {
                    $(btnKeyword).prop('disabled', false);
                }

            }
        }
    }
}

//Populate the 'Most Common' and 'Full List' Keyword tabs...
function populateKeywordTabs() {
    //Retrieve concept values from list...
    var listItems = $('#olConcepts li');
    var listArray = [];

    listItems.each(function (index, element) {

        var text = $(this).text();
        listArray.push(text);
    });

    if (0 < listArray.length) {
        //Re-populate 'Most Common' tab entries...
        $("input[name='keywords']").each(function (index, element) {
            var checkbox = $(this);
            if (-1 !== listArray.indexOf(checkbox.attr('value'))) {
                //Checkbox value found in concepts list - check the checkbox
                checkbox.prop('checked', true);
            }
        });


        //Re-populate 'Full List' tab entries...
        $("#tree").fancytree("getTree").visit(function (node) {
            if (-1 !== listArray.indexOf(node.title)) {
                //Node title found in concepts list - select the node
                node.setSelected(true);
            }
        });
    }
}


//Click handler for 'Common' keyword checkboxes
function clickCommonKeyword(event) {

    //Retrieve the checkbox value and state...
    var checked = $(event.target).is(':checked');
    var value = $(event.target).prop('value');

    //console.log('Value: ' + value + ' checked? ' + checked);

    //Update the associated value on the 'Full' tab, if indicated...
    $("#tree").fancytree("getTree").visit(function (node) {
        if (value.toLowerCase() === node.title.toLowerCase() && checked !== node.isSelected()) {
            node.setSelected(checked);
        }
    });

}

//Click handler for 'Select Data Service(s)' button
function clickSelectDataServices(event) {

    //If any data service(s) previously selected - select associated data table rows...
    mySelectedServices.length = 0;
    mySelectedServices = mySavedServices.slice(0);

    $('#checkOnlyObservedValues').prop('checked', bObservedValuesOnly);

    var rows = $("#dtServices").dataTable().fnGetNodes();
    var length = rows.length;
    var className = 'selected';
        
    for (var i = 0; i < length; ++i) {
        var id = $(rows[i]).find("td:eq(5)").html();

        if (-1 !== mySelectedServices.indexOf(id)) {
            $(rows[i]).addClass(className);            //Previously selected service Ids contain current id - select the row
        }
        else {
            $(rows[i]).removeClass(className);         //Previously selected service Ids DO NOT contain current id - DO NOT select the row
        }
    }

    enableDisableButton('dtServices', 'btnClearSelectionsDS');
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

    //Custom close button for streetview
    window.addEventListener('DOMContentLoaded', function (e) {

        // Get close button and insert it into streetView
        // #button can be anyt dom element
        var closeButton = document.querySelector('#StreetViewCloseButton'),
            controlPosition = google.maps.ControlPosition.RIGHT_CENTER;

        // Assumes map has been initiated 
        var streetView = map.getStreetView();

        // Hide useless and tiny default close button
        streetView.setOptions({ enableCloseButton: false });

        // Add to street view
        streetView.controls[controlPosition].push(closeButton);

        // Listen for click event on custom button
        // Can also be $(document).on('click') if using jQuery
        google.maps.event.addDomListener(closeButton, 'click', function () {
            streetView.setVisible(false);
        });

        //Add 'visible changed' listener for street view...
        //Source: http://stackoverflow.com/questions/7251738/detecting-google-maps-streetview-mode
        google.maps.event.addListener(streetView, 'visible_changed', function () {

            if (streetView.getVisible()) {
                // Street view is visible - show the custom close button
                $('#StreetViewCloseButton').show();
            } else {
                // Street view is NOT visible - hide the custom close button
                $('#StreetViewCloseButton').hide();
            }
        });

    });
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
            //console.log(obj);
        },
        hide: function (obj) {
            $("#map-canvas").position({ left: -$('#slider').width() })
            $("#map-canvas").width(($(window).width())) //setMapWidth

            google.maps.event.trigger(map, "resize");
            sidepanelVisible = false;
        },
        hidden: function (obj) {
            //console.log(obj);
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
        currentPlaceName = place.formatted_address;
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
    var area = GetAreaInSquareKilometers(path)
    return 'Area: ' + (area *= 1).toLocaleString() + ' sq km';
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
        if (json.features.length === 0)
        {
            //BCC - 29-Jun-2015 - QA Issue #29 - GUI: "No timeseries for specified parameters found" have different font size. 
            bootbox.alert("<h4>No timeseries for specified parameters found.</h4>")
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

            //BCC - 26-Jun-2015 - Conditionally enable 'Data' button... 
            //QA Issue #13 - Data tab (usability): if search doesn't return any results, data tab should be disabled
            var bMarkerFound = false;

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

                    //QA Issue #13
                    bMarkerFound = true;
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

                    //QA Issue #13
                    bMarkerFound = true;
                }
            };

            //QA Issue #13
            if (bMarkerFound) {
                $('.data').removeClass('disabled')
            }
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
    //if (checkReg2(startDate)==false) { bootbox.alert("Please validate your From: date"); return}
    var endDate = $("#endDate").val().toString();
    //if (checkReg2(endDate)==false) { bootbox.alert("Please validate your To: date"); return }
    var keywords = new Array();
    //variable.push  = $("#variable").val()

   //selected Services
    //var services = mySelectedServices;
    var services = mySavedServices;

    //alert(myTimeSeriesClusterDatatable.rows('.selected').data().length + ' row(s) selected');
    //only MuddyRiver
    //services.push(181);

    //BCC - 13-Jul-2015 - Since 'Common' and 'Full' keyword tab contents are kept in sync at all times, 
    //       take the selected keys from the 'Full' tab only...
    var tree = $("#tree").fancytree("getTree");
    var selKeys = $.map(tree.getSelectedNodes(true), function (node) { //true switch returns all top level nodes
        return node.title;
    }).join("##");

    keywords.push(selKeys)
                
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
            //BCC - 26-Jun-2015 - Conditionally enable 'Data' button... 
            //QA Issue #13 - Data tab (usability): if search doesn't return any results, data tab should be disabled
            //$('.data').removeClass('disabled');
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


//Attempt to retrieve the position name from the input marker's position...
//Source: https://developers.google.com/maps/documentation/javascript/examples/geocoding-reverse
//More information: https://developers.google.com/maps/documentation/javascript/geocoding
//                  https://developers.google.com/maps/articles/geocodestrat
function getMarkerPositionName(marker) {
    var geocoder = new google.maps.Geocoder();

    //NOTE: Asynchronous call - 
    //ASSUMPTION: This call returns before the user issues a time series request to the server...
    geocoder.geocode({ 'latLng': marker.getPosition() }, function (results, status) {
        if (status == google.maps.GeocoderStatus.OK) {
            if (results[0]) { //NOTE: Using the most detailed place name available...
                //Marker place name found - set variable
                currentMarkerPlaceName = results[0].formatted_address;
                $('#SeriesModal #myModalLabel').html('List of Timeseries near: ' + currentMarkerPlaceName );

            } else {
                //No marker place name found - reset variable
                currentMarkerPlaceName = '';
            }
        } else {
            //Failure - reset variable
            currentMarkerPlaceName = '';
        }
    });
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

            $('#SeriesModal #myModalLabel').html('List of Timeseries near Selected Marker');
            getMarkerPositionName(marker);

            setUpDatatables(clusterid);

            var table = $('#dtMarkers').dataTable();

            //BCC - 29-Jun-2015 - QA Issue # 26 - Data tab: filters under the timeseries table have no titles
            //Assign a handler to the DataTable 'draw.dt' event
            table.off('draw.dt', addFilterPlaceholders);
            table.on('draw.dt', { 'tableId': 'dtMarkers' }, addFilterPlaceholders);

            //BCC - 29-Jun-2015 - QA Issue #25 - Data tab: table column titles stay in wrong order after sorting table
            //Remove/add column adjustment event handler...
            $('#SeriesModal').off('shown.bs.modal', adjustColumns);
            $('#SeriesModal').on('shown.bs.modal', {'tableId': 'dtMarkers', 'containerId': 'SeriesModal'}, adjustColumns);

            $('#SeriesModal').modal('show');

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

            $('#SeriesModal #myModalLabel').html('List of Timeseries near Selected Marker');
            getMarkerPositionName(marker);

            setUpDatatables(clusterid);

            var table = $('#dtMarkers').dataTable();

            //BCC - 29-Jun-2015 - QA Issue # 26 - Data tab: filters under the timeseries table have no titles
            //Assign a handler to the DataTable 'draw.dt' event
            table.off('draw.dt', addFilterPlaceholders);
            table.on('draw.dt', { 'tableId': 'dtMarkers' }, addFilterPlaceholders);
                
            //BCC - 29-Jun-2015 - QA Issue #25 - Data tab: table column titles stay in wrong order after sorting table
            //Remove/add column adjustment event handler...
            $('#SeriesModal').off('shown.bs.modal', adjustColumns);
            $('#SeriesModal').on('shown.bs.modal', { 'tableId': 'dtMarkers', 'containerId': 'SeriesModal' }, adjustColumns);

            $('#SeriesModal').modal('show');

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

//BCC - 29-Jun-2015 - QA Issue # 26 - Data tab: filters under the timeseries table have no titles
//Prepend disabled options as 'placeholders' to filtering selects... 
//NOTE: When registered with the 'draw.dt', this function is called three times, once for each of the datatables sub-tables: header, body and footer.
//       The selector only works on the third call for the footer table...
//       Also, the function is called each time the user applies a filter. Thus the method always removes the 'placeholder' value from the select before prepending it to the select
//Source: http://stackoverflow.com/questions/22822427/bootstrap-select-dropdown-list-placeholder
function addFilterPlaceholders(event) {

    var tableId = event.data.tableId;
    //BCC - 10-Aug-2015 - GitHub Issue #35 - Add filter by Site Name
    var placeHolders = ['Organization', 'Service Code', 'Keyword', 'Variable Name', 'Site Name'];

    var selector = '#' + tableId + '_wrapper > div.dataTables_scroll > div.dataTables_scrollFoot > div > table > tfoot > tr > th > select';

    var selects = $(selector);

    //console.log('Select count: ' +selects.length);

    for(var i = 0; i < placeHolders.length; ++i) {
        var select = $(selects[i]);

        select.find("option[value='999999']").remove();

        if ('' === select.val()) {
            //No selection made - prepend 'placeholder' to selections
            select.prepend('<option value="999999" selected disabled style="color: grey;">Filter by: ' + placeHolders[i] + '</option>');
        }


    }
}


//BCC - 29-Jun-2015 - QA Issue #25 - Data tab: table column titles stay in wrong order after sorting table
//Force a Datatable redraw to render now visible headers in the correct widths...
//Source: http://datatables.net/reference/api/columns.adjust()
function adjustColumns(event) {

    var tableId = event.data.tableId;
    var containerId = event.data.containerId;

    var table = $(('#' + tableId)).DataTable();

    $(('#' + containerId)).css('display', 'block');
    table.columns.adjust().draw();
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

    if ($.fn.DataTable.isDataTable("#dtServices")) {
        $('#dtServices').DataTable().clear().destroy();
    }

    var actionUrl = "/home/getServiceList"
    // $('#demo').html( '<table cellpadding="0" cellspacing="0" border="0" class="display" id="example"></table>' );
 
    myServicesDatatable = $('#dtServices').dataTable({
        "ajax": actionUrl,
        "order": [2, 'asc'],
        "dom": 'l<"toolbarDS">frtip', //Add a custom toolbar - source: https://datatables.net/examples/advanced_init/dom_toolbar.html
         "columns": [

            { "data": "Organization" },
            { "data": "ServiceCode", "visible": false },
            { "data": "Title" },
            { "data": "DescriptionUrl", "visible": false },
            { "data": "ServiceUrl", "visible": false },
            { "data": "Checked", "visible": false },

            { "data": "Sites", "visible": true },
            { "data": "Variables", "visible": true },
            { "data": "Values", "visible": true },
           { "data": "ServiceBoundingBox", "visible": false },
            { "data": "ServiceID" }
         ],
         //"rowCallback": function( row, data ) {
         //    if ( $.inArray(data.DT_RowId, mySelectedServices) !== -1 ) {
         //        $(row).addClass('selected');
         //    }
         //},
        "createdRow": function (row, data, index) {

             var id = $('td', row).eq(5).html();
             var title = $('td', row).eq(1).html();
             var url = 'http://hiscentral.cuahsi.org/pub_network.aspx?n=';
             $('td', row).eq(1).html("<a href='" + url + id + "' target='_Blank'>" + title + " </a>");


         },
         //"scrollX": true,
         initComplete: function () {
             this.fnAdjustColumnSizing();
         }
    });
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

        enableDisableButton('dtServices', 'btnClearSelectionsDS');
    });

    //BC - Test - add a custom toolbar to the table...
    //source: https://datatables.net/examples/advanced_init/dom_toolbar.html
    $("div.toolbarDS").html('<input type="button" style="margin-left: 2em; float:left;" class="btn btn-warning" disabled id="btnClearSelectionsDS" value="Clear Selection(s)"/>');


    //Avoid multiple registrations of the same handler...
    $('#btnClearSelectionsDS').off('click', clearServicesSelections);
    $('#btnClearSelectionsDS').on('click', { 'tableId': 'dtServices', 'btnId': 'btnClearSelectionsDS' }, clearServicesSelections);

    //return table;
}
//Datatable for Marker
function setUpDatatables(clusterid)
{
    
//    var dataSet = [
//    ['Trident','Internet Explorer 4.0','Win 95+','4','X'],
//    ['Trident','Internet Explorer 5.0','Win 95+','5','C'],
 
    //];
    if ($.fn.DataTable.isDataTable("#dtMarkers")) {
        $('#dtMarkers').DataTable().clear().destroy();
    }
    
    //Reset the selected row count...
    selectedRowCounts['dtMarkers'].count = 0;


    //var dataSet = getDetailsForMarker(clusterid)
    var actionUrl = "/home/getDetailsForMarker/" + clusterid
   // $('#example').DataTable().clear()
   // $('#demo').html( '<table cellpadding="0" cellspacing="0" border="0" class="display" id="example"></table>' );
 
    var oTable= $('#dtMarkers').dataTable({
        "ajax": actionUrl,
        //"autoWidth": true,    //BCC - 29-Jun-2015 - property defaults to true - no need to set here
        //"jQueryUI": false,    //BCC - 29-Jun-2015 - property defaults to false - no need to set here
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
            { "data": "ServCode", "sTitle": "Service Code", "visible": true },
            { "data": "ConceptKeyword", "sTitle": "Keyword", "visible": true },
            { "data": "ServURL", "visible": false },
            { "data": "VariableName", "width": "50px", "sTitle": "Variable Name" },
            //BCC - 10-Jul-2015 - Internal QA Issue #29 - Include VariableCode and SiteCode
            { "data": "SiteCode", "sTitle": "Site Code", "visible": true },
            { "data": "VariableCode", "sTitle": "Variable Code", "visible": true },
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
            { "data": "SeriesId" },
            //BCC - 10-Jul-2015 - Add links to Description URL and Service URL (WSDL)
            { "data": null, "sTitle": "Service URL", "visible": true },
            { "data": "ServURL", "sTitle": "Web Service Description URL", "visible": true }
         ],

         "scrollX": true,
         
         "createdRow": function (row, data, index) {

                 //Create a link to the Service URL
                 var org = $('td', row).eq(0).html();
                 var servCode = $('td', row).eq(1).html();

                 var descUrl = getDescriptionUrl(servCode);
                 $('td', row).eq(17).html("<a href='" + descUrl + "' target='_Blank'>" + org + " </a>");

                 //Create a link to the Web Service Description URL...
                 var servUrl = $('td', row).eq(18).html();
                 $('td', row).eq(18).html("<a href='" + servUrl + "' target='_Blank'>" + org + " </a>");

                 //If the new row is in top <selectedTimeSeriesMax>, mark the row as selected per check box state...
                 if ($('#chkbxSelectAll').prop('checked')) {
                     //BC - Call check box hander here
                     $('#chkbxSelectAll').triggerHandler('click');
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

            //BCC - 10-Aug-2015 - GitHub Issue #35 - Add filter by Site Name 
            setfooterFilters('dtMarkers', [0, 1, 2, 4, 10], 'chkbxSelectAll');

            //BC - 10-Jul-2015 - Temporarily disable tooltips...
            //setUpTooltips('dtMarkers');
        
            oTable.fnAdjustColumnSizing();
        }
        
         
        //"retrieve": true
    });


    //Suppress DataTable display of server errors as alerts...
    $.fn.dataTable.ext.errMode = 'none',

    //Server-error handler for dtMarkers...
    $('#dtMarkers').on('error.dt', function (event, settings, techNote, message) {

        //Close current dialog - open 'Server Error' dialog
        $('#SeriesModal').modal('hide');

        $('#serverErrorModal').modal('show');

        //Log messsage received from server...
        console.log('dtMarkers reports error: ' + message);
    });


    //workaround reorder to align headers

    //BC - Test - make each table row selectable by clicking anywhere on the row...
    //Source: https://datatables.net/examples/api/select_row.html
    //Avoid multiple registrations of the same handler...
    $('#dtMarkers tbody').off('click', 'tr', toggleSelected);
    $('#dtMarkers tbody').on('click', 'tr', {'tableId': 'dtMarkers', 'btnId': 'btnZipSelections', 'btnClearId': 'btnClearSelections'},toggleSelected);

    //BC - Test - add a custom toolbar to the table...
    //source: https://datatables.net/examples/advanced_init/dom_toolbar.html
    $("div.toolbar").html('<span style="float: left; margin-left: 1em;" id="spanSelectAll"><input type="checkbox" class="ColVis-Button" id="chkbxSelectAll" style="float:left;"/>&nbsp;Select Top ' + 
                          selectedTimeSeriesMax.toString() + '?</span>' +
                          '<input type="button" style="margin-left: 2em; float:left;" class="btn btn-warning" disabled id="btnClearSelections" value="Clear Selection(s)"/>' +
                          '<input type="button" style="margin-left: 2em; float:left;" class="ColVis-Button btn btn-primary" disabled id="btnZipSelections" value="Process Selection(s)"/>' +
                          '<span class="clsZipStarted" style="display: none; float:left; margin-left: 2em;"></span>');
    //$("div.toolbar").css('border', '1px solid red');
  
    // BC - Do not respond to data table load event...
    //Add data load event handler...
    //$('#dtMarkers').off('xhr.dt', dataTableLoad);
    //$('#dtMarkers').on('xhr.dt', { 'chkbxId': '#chkbxSelectAll'}, dataTableLoad);

    //Add click handlers...

    //Avoid multiple registrations of the same handler...
    $('#chkbxSelectAll').off('click', selectAll);
    $('#chkbxSelectAll').on('click', { 'tableId': 'dtMarkers', 'chkbxId': 'chkbxSelectAll', 'btnId': 'btnZipSelections', 'btnClearId': 'btnClearSelections'}, selectAll);

    //Avoid multiple registrations of the same handler...
    $('#btnZipSelections').off('click', zipSelections);
    $('#btnZipSelections').on('click', { 'tableId': 'dtMarkers', 'chkbxId': 'chkbxSelectAll' }, zipSelections);

    //Avoid multiple registrations of the same handler...
    $('#btnClearSelections').off('click', clearSelections);
    $('#btnClearSelections').on('click', { 'tableId': 'dtMarkers', 'chkbxId': 'chkbxSelectAll', 'btnId': 'btnZipSelections', 'btnClearId': 'btnClearSelections' }, clearSelections);

    //Add DataTables event handlers...
    //Search event...
    $('#dtMarkers').off('search.dt', dtSearchOrOrder);
    $('#dtMarkers').on('search.dt', { 'tableId': 'dtMarkers', 'chkbxId': 'chkbxSelectAll' }, dtSearchOrOrder);

    //Order event...
    $('#dtMarkers').off('order.dt', dtSearchOrOrder);
    $('#dtMarkers').on('order.dt', { 'tableId': 'dtMarkers', 'chkbxId': 'chkbxSelectAll' }, dtSearchOrOrder);


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


//Retrieve the Description Url from the Services DataTable for the input service code- Not found, return null
//Assumption: The services DataTable is loaded immediately upon page load or refresh!!
function getDescriptionUrl(serviceCode) {

    var descriptionUrl = null;
    if (('undefined' !== typeof serviceCode) && (null !== serviceCode)) {
        
        var table = $('#dtServices').DataTable();
        var data = table.rows().data();

        $.each(data, function (i, obj) {
            if (serviceCode === obj.ServiceCode) {
                descriptionUrl = obj.DescriptionUrl;
                return false;
            }
        });
    }

    //Processing complete - return
    return descriptionUrl;
}

//Create 'select'-based filters for the input tableId and columns array
function setfooterFilters(tableId, columnsArray, chkbxId) {

    var api = $('#' + tableId).DataTable();

    api.columns().indexes().flatten().each(function (i) {
        if (-1 !== columnsArray.indexOf(i)) {
            var column = api.column(i);
            var data = column.data();
            //var select = $('<select><option value=""></option></select>')
            var select = $('<select><option value="">Remove filter...</option></select>')
                .appendTo($(column.footer()).empty())
                .on('change', function () {
                    var val = $.fn.dataTable.util.escapeRegex($(this).val());

                    var dt = column.search(val ? '^' + val + '$' : '', true, false);
                    dt.draw();

                    //If 'Select Top' checkbox is checked, trigger the associated handler(s)
                    if ($('#' + chkbxId).prop('checked')) {
                        $('#' + chkbxId).triggerHandler('click');
                    }
                });

            column.data().unique().sort().each(function (d, j) {
                select.append('<option value="' + d + '">' + d + '</option>')
            });
        }
    });

    $('#' + tableId).dataTable().fnAdjustColumnSizing();
}

function updateServicesList()
{
    //Clear and re-populate services list...
    var list = $('#olServices');
    var length = mySavedServices.length;
    

    list.empty();

    if (0 < length) {
        //Certain services selected...
        list.append('<li>Selected ' + length.toString() + ' of ' + myServicesDatatable.fnGetData().length + '</li>');
    }
    else {
        //All services selected...
        //list.append('Selected All of ' + myServicesDatatable.fnGetData().length + '');
        list.append('<li>All</li>');
    }
}

function toggleSelected(event) {
    var className = 'selected';
    var nonClassName = 'notSelected';

    var startRange = 'startRange';
    var endRange = 'endRange';

    //Check selected row count...
    var count = selectedRowCounts[event.data.tableId].count;

    if ((selectedTimeSeriesMax <= count) && (! $(this).hasClass(className))) {
        //Maximum selections reached - current row not yet selected - warn and return early
        bootbox.alert('<h4>A maximum of ' + selectedTimeSeriesMax + ' time series rows may be selected.</h4>');
        return;
    }

    //Retrieve current table rows as jqueryObjects...
    var jqueryRows = $('#' + event.data.tableId).DataTable().rows().nodes().to$();
    var bStart = jqueryRows.hasClass(startRange);

    if (event.shiftKey && (!$(this).hasClass(className)) && bStart) {
        //User has pressed SHIFT+click - current row not yet selected - 'start range' assigned to another row - process range selection
        $(this).addClass(endRange);
        processRangeSelection(event.data.tableId, startRange, endRange, className, nonClassName);
        return;
    } 

    $(this).toggleClass(className);

    //Update selected row count...
    //selectedRowCounts[event.data.tableId].count = $(this).hasClass(className) ? count + 1 : count - 1;
    if ($(this).hasClass(className)) {
        selectedRowCounts[event.data.tableId].count = count + 1;
        $(this).removeClass(nonClassName);  //Remove manually unselected indicator...

        //Set current row as 'start of range'
        jqueryRows.removeClass(startRange);
        $(this).addClass(startRange);

    } else {
        selectedRowCounts[event.data.tableId].count = count - 1;
        $(this).addClass(nonClassName);     //Mark row as manually unselected by user...

        //Remove 'range' classes, if any
        $(this).removeClass(startRange);
        $(this).removeClass(endRange);
    }

    //Check state of 'Process Selections' button...
    enableDisableButton(event.data.tableId, event.data.btnId);
    enableDisableButton(event.data.tableId, event.data.btnClearId);
}

//Process the range selection for the input table Id
function processRangeSelection(tableId, startRangeClass, endRangeClass, selectClass, unSelectClass) {
    //alert('processRangeSelection called...');
    
    //Validate/initialize input parameters...
    if (( 'undefined' === typeof tableId) || (null === tableId) ||
        ('undefined' === typeof startRangeClass) || (null === startRangeClass) ||
        ('undefined' === typeof endRangeClass) || (null === endRangeClass) ||
        ('undefined' === typeof selectClass) || (null === selectClass) ||
        ('undefined' === typeof unSelectClass) || (null === unSelectClass)) {
        return;
    }

    //Check for presence of 'startRange' and 'endRange' classes on table rows...
    var table = $('#' + tableId).DataTable();
    var rows = table.rows({ 'order': 'current', 'search': 'applied' });    //Retrieve rows per current sort/search order...
    var jqueryRows = rows.nodes().to$();

    var startRow = table.row('.' + startRangeClass);
    var endRow = table.row('.' + endRangeClass);

    if ((0 >= startRow.length) || (0 >= endRow.length)) {
        //Class(es) not found - return
        return;
    }

    //Determine start and end range positions
    var startPos = rows[0].indexOf(startRow.index());
    var endPos = rows[0].indexOf(endRow.index());

    //Verify start and end range positions are on the current table 'page'...
    var pageInfo = table.page.info();

    if (((startPos < endPos) && (startPos < pageInfo.start || endPos > pageInfo.end)) ||
        ((startPos > endPos) && (endPos < pageInfo.start || startPos > pageInfo.end))) {
        //Start and/or end range positions NOT on current table 'page' - warn, remove start and end range classe, return
        bootbox.alert("Multiple row selection limited to the current table 'page'.");

        jqueryRows.removeClass(startRangeClass);
        jqueryRows.removeClass(endRangeClass);

        return;
    }

    //Check 'rows to select' count and current rows selected count against allowed maximum...
    var toSelect = Math.abs(endPos - startPos);
    var count = selectedRowCounts[tableId].count;

    if (selectedTimeSeriesMax < (toSelect + count)) {
        //Allowed maximum exceeded - warn, remove start and end range classes, return
        bootbox.alert('Specified range and selected rows (' + (toSelect + count) + ') exceed the maximum allowed (' + selectedTimeSeriesMax + ')');

        jqueryRows.removeClass(startRangeClass);
        jqueryRows.removeClass(endRangeClass);

        return;
    }

    //All rows in specified range selectable, update count
    selectedRowCounts[tableId].count = toSelect + count;
    
    //Remove start and end range classes
    jqueryRows.removeClass(startRangeClass);
    jqueryRows.removeClass(endRangeClass);

    //Apply 'select' class to range 
    var lower = startPos;
    var upper = endPos;

    if (startPos > endPos) {
        //Reverse range - adjust lower and upper values
        lower = endPos;
        upper = startPos
    }

    var length = jqueryRows.length;
    for (var i = 0; i < length; ++i) {
        var pos = rows[0].indexOf(jqueryRows[i]._DT_RowIndex);

        if (pos >= lower && pos <= upper) {
            var jqueryObject = $(jqueryRows[i]);

            jqueryObject.removeClass(unSelectClass);
            jqueryObject.addClass(selectClass);
        }
    }
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

//BCC - 30-Jun-2015 - Revised formatting for QA issue #32 - Download Manager (GUI): table entries arу scattered all over the page
//Add styles to row for download manager table...
function addRowStylesDM(newrow) {

    //Cell: 0
    var td = newrow.find('td:eq(0)');
    td.addClass('text-center');
    td.css({ 'vertical-align': 'middle', 'display': 'none' });

    //Cell: 1
    td = newrow.find('td:eq(1)');
    td.addClass('text-center');
    td.css({ 'vertical-align': 'middle', 'height': '3em', 'width': '15%' });

    //Cell: 2
    td = newrow.find('td:eq(2)');
    td.addClass('text-center');
    td.css({ 'vertical-align': 'middle', 'height': '3em', 'width': '25%' });

    //Cell: 3
    td = newrow.find('td:eq(3)');
    td.addClass('text-center');
    //td.attr('title', 'zip archive URL goes here');
    td.css({ 'vertical-align': 'middle', 'height': '3em', 'width': '40%', 'overflow': 'hidden'});

    //Cell: 4
    td = newrow.find('td:eq(4)');
    td.addClass('text-center');
    td.css({ 'vertical-align': 'middle', 'height': '3em', 'width': '13%'});
}

function selectAll(event) {

    //Clear ALL table selections, regardless of current sort/search order
    var table = $('#' + event.data.tableId).DataTable();
    var rows = table.rows();                    //Retrieve ALL rows
    var nodesRendered = rows.nodes();           //Retrieve all the rendered nodes for these rows
                                                //NOTE: Rendered nodes retrieved in the same order as the rows...
    var jqueryObjects = nodesRendered.to$();    //Convert to jQuery Objects!!
    var className = 'selected';
    var nonClassName = 'notSelected';

    //Remove selected class from all rendered rows...
    jqueryObjects.removeClass(className);

    //Apply 'selected' class to the 'top' <selectedTimeSeriesMax> rendered nodes, if indicated 
    if ($('#' + event.data.chkbxId).prop("checked")) {
        //Retrieve all the table's RENDERED <tr> elements whether visible or not, in the current sort/search order...
        //Source: http://datatables.net/reference/api/rows().nodes()
        rows = table.rows({ 'order': 'current', 'search': 'applied' });    //Retrieve rows per current sort/search order...
        var totalRows = rows[0].length;

        nodesRendered = rows.nodes();                                    //Retrieve all the rendered nodes for these rows
        var length = nodesRendered.length;

        //Initialize the selected row count...
        var count = totalRows < selectedTimeSeriesMax ? totalRows : selectedTimeSeriesMax;

        //For each rendered node...
        for (var i = 0; i < length; ++i) {

            //Determine the position of the associated row in the current sort/search order...
            var position = rows[0].indexOf(nodesRendered[i]._DT_RowIndex)
            
            if (position < selectedTimeSeriesMax) {
                //Row is within the 'top' <selectedTimeSeriesMax> - apply class, if indicated
                var jqueryObject = $(nodesRendered[i]);

                if (null !== jqueryObject) {
                    //Check for a manually unselected row, skip if found...
                    if (jqueryObject.hasClass(nonClassName)) {
                        --count;    
                        continue;
                    }

                    jqueryObject.addClass(className);   //Apply class...
                }
            }
        }

        selectedRowCounts[event.data.tableId].count = count;
    }
    else {
        //Reset the selected row count...
        selectedRowCounts[event.data.tableId].count = 0;
    }

    //Check state of 'Process Selections' button...
    enableDisableButton(event.data.tableId, event.data.btnId);
    enableDisableButton(event.data.tableId, event.data.btnClearId);
}

//Clear --ALL-- table selections, regardless of current sort/search order...
function clearSelections(event) {

    //Retrieve all the table's RENDERED <tr> elements whether visible or not, in the current sort/search order...
    //Source: http://datatables.net/reference/api/rows().nodes()
    var table = $('#' + event.data.tableId).DataTable();
//    var rows = table.rows({ 'order': 'current', 'search': 'applied' });     //Retrieve rows per current sort/search order...
    var rows = table.rows();     //Retrieve ALL rows
    var nodesRendered = rows.nodes();                                       //Retrieve all the rendered nodes for these rows
                                                                            //NOTE: Rendered nodes retrieved in the same order as the rows...
    var jqueryObjects = nodesRendered.to$();    //Convert to jQuery Objects!!
    var className = 'selected';
    var nonClassName = 'notSelected';

    //Remove selected class from all rendered rows...
    jqueryObjects.removeClass(className);
    jqueryObjects.removeClass(nonClassName);

    //Reset the selected row count...
    selectedRowCounts[event.data.tableId].count = 0;

    //Uncheck the 'top ...' checkbox
    $('#' + event.data.chkbxId).prop("checked", false);

    //Check button states...
    enableDisableButton(event.data.tableId, event.data.btnId);
    enableDisableButton(event.data.tableId, event.data.btnClearId);
}

//Clear all table selections...
function clearServicesSelections(event) {
    //Retrieve all the table's RENDERED <tr> elements whether visible or not, in the current sort/search order...
    //Source: http://datatables.net/reference/api/rows().nodes()
    var table = $('#' + event.data.tableId).DataTable();
    var rows = table.rows({ 'order': 'current', 'search': 'applied' });     //Retrieve rows per current sort/search order...
    var nodesRendered = rows.nodes();                                       //Retrieve all the rendered nodes for these rows
    //NOTE: Rendered nodes retrieved in the same order as the rows...
    var jqueryObjects = nodesRendered.to$();    //Convert to jQuery Objects!!
    var className = 'selected';

    //Remove selected class from all rendered rows...
    jqueryObjects.removeClass(className);

    //Clear the selected services array
    mySelectedServices.length = 0;

    //Uncheck the 'Select all non-gridded...' checkbox
    $("input[name='checkOnlyObservedValues']").attr('checked', false);

    //Check button state...
    enableDisableButton(event.data.tableId, event.data.btnId);
}

//Set/reset 'disabled' attribute on referenced button per contents of referenced Data Table...
function enableDisableButton(tableId, btnId) {

    var table = $('#' + tableId).DataTable();
    var selectedRows = table.rows('.selected').data();
    var selectedCount = selectedRows.length;

    if (0 < selectedCount) {
        $('#' + btnId).prop('disabled', false);
    }
    else {
        $('#' + btnId).prop('disabled', true);
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
//function dataTableLoad(event, settings, json, xhr) {

//    if ((null !== json) && (null !== json.data)) {
//        //Successful AJAX query - check total rows received against max. selectable row count...
//        var length = json.data.length;
//        var propValue = (selectedTimeSeriesMax >= length) ? false : true;

//        //$(event.data.chkbxId).prop('disabled', propValue);

//        //Remove the tooltip and title attributes...
//        $(event.data.chkbxId).removeAttr('data-toggle data-placement title');
//        $(event.data.chkbxId).removeClass('disabled');
//        //$(event.data.chkbxId).prop('disabled', propValue);

//        if (true === propValue) {
//            //Checkbox disabled - add disabled class 
//            $(event.data.chkbxId).addClass('disabled');

//            //Add tooltip explaining why
//            $(event.data.chkbxId).attr('data-toggle', 'tooltip');
//            $(event.data.chkbxId).attr('data-placement', 'right');
//            $(event.data.chkbxId).attr('title', 'The total timeseries rows (' + length.toString() + ') exceed the selectable maximum (' + selectedTimeSeriesMax.toString() + ')');

//            //Enable Bootstrap tooltips...
//            $('[data-toggle="tooltip"]').tooltip();
//        }
//    }
//}

function zipSelections(event) {

    //Get the next Task Id...
    var taskId = getNextTaskId();
    var txtClass = '.clsZipStarted';

    //Update the text...
    var txt = 'Task: @@taskId@@ started.  To check the download status, please open Download Manager';
    var txts = txt.split('@@taskId@@');

    $(txtClass).text( txts[0] + taskId.toString() + txts[1]);

    //Display the 'Zip processing started... messag e
    displayAndFadeLabel(txtClass);

    //Create the list of selected time series ids...
    //var table = $('#dtMarkers').DataTable();
    var table = $('#' + event.data.tableId).DataTable();
    var selectedRows = table.rows('.selected').data();

    if ($('#' + event.data.chkbxId).prop("checked") && (selectedTimeSeriesMax > selectedRows.length)) {
        //User has clicked the 'Select Top ...' check box but not all selected rows have been rendered
        //NOTE: If the DataTable instance has the 'deferRender' option set - not all rows may have been rendered at this point.
        //        Thus one cannot rely on the selectedRows above, since in this case only rendered rows appear in the selectedRows...

        //Scan all table rows for the 'Select Top ...' rows...
        //var positions = table.rows()[0];
        //var rows = table.rows().data();

        //var positions = table.rows({'order': 'current', 'search': 'applied'})[0];   //Retrieve rows per current sort/search order...
        //var rows = table.rows({'order' : 'current', 'search': 'applied'}).data();   //Retrieve rows per current sort/search order...
        var rows = table.rows({ 'order': 'current', 'search': 'applied' });           //Retrieve rows per current sort/search order...

        var length = rows[0].length;    //Need length of rows array here!!
        selectedRows = [];

        for (var i = 0; i < length; ++i) {
            //var position = positions.indexOf(i);
            //var position = i;
            var position = rows[0].indexOf(i);

            //If row is rendered, check if selected...
            var node = table.row(i).node();
            if (null !== node) {
                var jqueryObj = $(node); 
                if (jqueryObj.hasClass('notSelected')) {
                    continue;   //Row rendered and NOT selected, continue to next row...
                }
            }

            if (position < selectedTimeSeriesMax) {
                //Current row position within 'Select Top ...' - append row data to selected rows...
                selectedRows.push(table.row(i).data());
            }
        }
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

    //Set request name from marker place name (more precise) --OR-- place name (less precise)...
    var regexp = /\s*[, ]\s*/g;
    if (('undefined' !== typeof currentMarkerPlaceName) && (null !== currentMarkerPlaceName) && ('' !== currentMarkerPlaceName)) {
        requestName = currentMarkerPlaceName.replace(regexp, '-');  //Replace whitespace and commas with '_' 
    }
    else {
        if (('undefined' !== typeof currentPlaceName) && (null !== currentPlaceName) && ('' !== currentPlaceName)) {
            requestName = currentPlaceName.replace(regexp, '-');  //Replace whitespace and commas with '_' 
        }
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
        context: timeSeriesRequestString,
        success: function (data, textStatus, jqXHR) {
            //Retrieve response data
            var response = jQuery.parseJSON(data);

            //alert("Response received: " + response.Status.toString());

            //Add new row to the download manager table
            //BCC - 30-Jun-2015 - Revised formatting for QA issue #32 - Download Manager (GUI): table entries arу scattered all over the page
            var cols = [];
            var div = '';

            //Column 0
            cols.push(response.RequestId.toString());

            //Column 1
            div = $('<div class="btn" style="font-size: 1em; font-weight: bold; margin: 0 auto;">' + taskId + '</div>');
            cols.push(div);

            //Column 2
            div = $('<div class="btn" style="font-size: 1em; font-weight: bold; margin: 0 auto;">' + formatStatusMessage(response.Status) + '</div>');
            cols.push(div);

            //Column 3
            div = $('<div class="btn" id="blobUriText" style="font-size: 1em; font-weight: bold; margin: 0 auto; text-overflow: ellipsis;">' + response.BlobUri + '</div>');
            cols.push(div);

            //Column 4
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
                        context: response.RequestId,
                        success: function (data, textStatus, jqXHR) {

                        var statusResponse = jQuery.parseJSON(data);

                            //Update status in download manager table...
                            //var tableRow = $("#tblServerTaskCart > td").filter(function () {
                        var tableRow = $("#tblDownloadManager tr td").filter(function () {
                            return $(this).text() === statusResponse.RequestId;
                        }).parent("tr");

                        //tableRow.find('td:eq(2)').html(statusResponse.Status);
                        //tableRow.find('td:eq(3)').html(statusResponse.BlobUri);
                        tableRow.find('#statusMessageText').html(statusResponse.Status);
                        tableRow.find('#blobUriText').html(statusResponse.BlobUri);
                        tableRow.find('td:eq(5)').html(statusResponse.RequestStatus);

                        //Color table row as 'warning'
                        tableRow.removeClass('info');
                        tableRow.addClass('warning');
                        },
                        error: function (xmlhttprequest, textStatus, message) {
                            //alert('Failed to request server to stop task ' + message);

                            //Update row with Stop Task Error status
                            var requestId = this.valueOf();   //From context: parameter on ajax call - convert string 'object' to string 'primitive'...
                            var errorDescription = (timeSeriesRequestStatus.properties[timeSeriesRequestStatus.StopTaskError]).description;

                            var tableRow = $("#tblDownloadManager tr td").filter(function () {
                                return $(this).text() === requestId;
                            }).parent("tr");

                            tableRow.find('#statusMessageText').html(errorDescription);
                            tableRow.find('#blobUriText').html('');
                            tableRow.find('td:eq(5)').html(timeSeriesRequestStatus.StopTaskError);

                            //Change the glyphicon and stop the animation
                            var glyphiconSpan = tableRow.find('#glyphiconSpan');

                            glyphiconSpan.removeClass('glyphicon-refresh spin');
                            glyphiconSpan.addClass('glyphicon-thumbs-down');


                            //Color table row as 'warning'
                            tableRow.removeClass('info');
                            tableRow.addClass('warning');
                        }
                    });
            });
            
            //Column 4 (cont'd) 
            cols.push(button);

            //Column 5
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
                    context: response.RequestId,
                    cache: false,   //So IE does not cache when calling the same URL - source: http://stackoverflow.com/questions/7846707/ie9-jquery-ajax-call-first-time-doing-well-second-time-not
                    success: function (data, textStatus, jqXHR) {

                        var statusResponse = jQuery.parseJSON(data);

                        //Retrieve associated row from Download Manager table...
                        //var tableRow = $("#tblServerTaskCart > td").filter(function () {
                        var tableRow = $("#tblDownloadManager tr td").filter(function () {
                            return $(this).text() === response.RequestId;
                        }).parent("tr");

                        //Check current status...
                        var statusCurrent = parseInt(tableRow.find('td:eq(5)').html());
                        if ((statusCurrent === timeSeriesRequestStatus.RequestTimeSeriesError) ||
                            (statusCurrent === timeSeriesRequestStatus.CheckTaskError) ||
                            (statusCurrent === timeSeriesRequestStatus.StopTaskError)) {
                            //Error status - stop interval - return
                            clearInterval(intervalId);
                            return;
                        }

                        //Update table row...
                        //tableRow.find('td:eq(1)').html(statusResponse.Status);
                        //tableRow.find('td:eq(1)').html(formatStatusMessage(statusResponse.Status));
                        tableRow.find('#statusMessageText').html(statusResponse.Status);
                        //tableRow.find('td:eq(3)').html(statusResponse.BlobUri);
                        tableRow.find('#blobUriText').html(statusResponse.BlobUri);
                        tableRow.find('td:eq(5)').html(statusResponse.RequestStatus);

                        //Color row as 'info'
                        tableRow.addClass('info');

                        //Check for completed/cancelled task
                        var requestStatus = parseInt(tableRow.find('td:eq(5)').html());

                        if (timeSeriesRequestStatus.Completed === requestStatus ||
                            timeSeriesRequestStatus.CanceledPerClientRequest === requestStatus ||
                            timeSeriesRequestStatus.ProcessingError === requestStatus) {
                            //If task completed - re-assign 'Stop Task' button to 'Download'
                            if (timeSeriesRequestStatus.Completed === requestStatus) {
                                //Success - retrieve the base blob URI... 
                                var baseUri = (statusResponse.BlobUri).split('.zip');
                                var blobUri = baseUri[0] += '.zip';

                                //Retrieve the full file name from the base blob URI...
                                var uriComponents = baseUri[0].split('/');
                                var fileName = uriComponents[(uriComponents.length - 1)];

                                //Set base blob URI into text in column 3
                                //tableRow.find('td:eq(3)').attr('title', baseUri[0]);
                                tableRow.find('#blobUriText').html(fileName);

                                //Create a download button...
                                var button = tableRow.find('td:eq(4)');

                                button = $("<button class='zipBlobDownload btn btn-success' style='font-size: 1.5vmin' data-blobUri='" + blobUri +  "'>Download Archive</button>");

                                button.on('click', function (event) {
                                    //var blobUri = tableRow.find('td:eq(3)').html();
                                    //var blobUri = tableRow.find('#blobUriText').html();
                                    var blobUri = $(this).attr('data-blobUri');
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
                                if (timeSeriesRequestStatus.CanceledPerClientRequest === requestStatus) {
                                    //Task canceled - color row as 'danger'
                                    tableRow.addClass('danger');

                                    //Fade row and remove from table...
                                    tableRow.fadeTo(1500, 0.5, function () {
                                        $(this).remove();
                                    });
                                }
                            }
                            
                            //Clear the interval
                            clearInterval(intervalId);
                            return;
                        }
                    },
                    error: function (xmlhttprequest, textStatus, message) {

                        //For now - take no action...
                        //ASSUMPTION: Next request will succeed
                        //var n = 6;
                        //n++;

                        //alert('Failed to retrieve task status ' + message);

                        //TO DO - can call clearInterval here to stop requesting status 
                        //          for a task that has errored...
                    }
                });

            }, 1000);

        },
        error: function (xmlhttprequest, textstatus, message) {
            //alert('Failed request time series: ' + message);

            //TO DO - Replace this code with logic to update row in Download Manager 
            //          table with new Enum value - RequestTimeSeries failure...
            //Server error: Close modal dialog, if indicated - open 'Server Error' dialog
            if ($('#SeriesModal').is(":visible")) {
                $('#SeriesModal').modal('hide');
            }

            $('#serverErrorModal').modal('show');

            //Log messsage received from server...
            console.log('RequestTimeSeries reports error: ' + message);
        }
    });
}

//Format the html for the status message as follows - all elements inline:  <h3> - glyphicon spinner </h3> <span> status message text </span> 
function formatStatusMessage(statusText) {
    
    var formattedMessage = '<h3 class="text-center" style="display: inline; vertical-align: middle;">' + 
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

//Data table for data tab
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

    //Reset the selected row count...
    selectedRowCounts['dtTimeseries'].count = 0;

    //Set page title...
//    $('#dataview #myModalLabel').html('List of Timeseries near Selected Area');
    $('#dataview #myModalLabel').html('List of Timeseries near: ' + currentPlaceName);
        
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
            { "data": "ServCode", "sTitle": "Service Code", "visible": true },
            { "data": "ConceptKeyword", "sTitle": "Keyword", "visible": true },
            { "data": "ServURL", "visible": false },
            { "data": "VariableName", "width": "50px", "sTitle": "Variable Name" },
            //BCC - 10-Jul-2015 - Internal QA Issue #29 - Include VariableCode and SiteCode
            { "data": "SiteCode", "sTitle": "Site Code", "visible": true },
            { "data": "VariableCode", "sTitle": "Variable Code", "visible": true },
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
            { "data": "SeriesId" },
            //BCC - 10-Jul-2015 - Add links to Description URL and Service URL (WSDL)
            { "data": null, "sTitle": "Service URL", "visible": true },
            { "data": "ServURL", "sTitle": "Web Service Description URL", "visible": true }
           ],
        "scrollX": true,
        "createdRow": function (row, data, index) {

            //Create a link to the Service URL
            var org = $('td', row).eq(0).html();
            var servCode = $('td', row).eq(1).html();

            var descUrl = getDescriptionUrl(servCode);
            $('td', row).eq(17).html("<a href='" + descUrl + "' target='_Blank'>" + org + " </a>");

            //Create a link to the Web Service Description URL...
            var servUrl = $('td', row).eq(18).html();
            $('td', row).eq(18).html("<a href='" + servUrl + "' target='_Blank'>" + org + " </a>");

            //If the new row is in top <selectedTimeSeriesMax>, mark the row as selected per check box state...
            if ($('#chkbxSelectAllTS').prop('checked')) {
                //BC - Call check box hander here
                $('#chkbxSelectAllTS').triggerHandler('click');
            }

            //if (data[0].replace(/[\$,]/g, '') * 1 > 250000) {

            //var id = $('td', row).eq(0).html();
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
        "initComplete": function () {

            //BCC - 10-Aug-2015 - GitHub Issue #35 - Add filter by Site Name
            setfooterFilters('dtTimeseries', [0, 1, 2, 4, 10], 'chkbxSelectAllTS');

            oTable.fnAdjustColumnSizing();
        }

    });
      
    //BC - Test - make each table row selectable by clicking anywhere on the row...
    //Source: https://datatables.net/examples/api/select_row.html
    //Avoid multiple registrations of the same handler...
    $('#dtTimeseries tbody').off('click', 'tr', toggleSelected);
    $('#dtTimeseries tbody').on('click', 'tr', { 'tableId': 'dtTimeseries', 'btnId': 'btnZipSelectionsTS', 'btnClearId': 'btnClearSelectionsTS' }, toggleSelected);

    //BC - Test - add a custom toolbar to the table...
    //source: https://datatables.net/examples/advanced_init/dom_toolbar.html
    $("div.toolbarTS").html('<span style="float: left; margin-left: 1em;"><input type="checkbox" class="ColVis-Button" id="chkbxSelectAllTS" style="float:left;"/>&nbsp;Select Top ' +
                            selectedTimeSeriesMax.toString() + '?</span>' +
                            '<input type="button" style="margin-left: 2em; float:left;" class="btn btn-warning" disabled id="btnClearSelectionsTS" value="Clear Selection(s)"/>' +
                            '<input type="button" style="margin-left: 2em; float:left;" class="ColVis-Button btn btn-primary" disabled id="btnZipSelectionsTS" value="Process Selection(s)"/>' +
                            '<span class="clsZipStarted" style="display: none; float:left; margin-left: 2em;"></span>');

    // BC - Do not respond to data table load event...
    //Add data load event handler...
    //$('#dtTimeseries').off('xhr.dt', dataTableLoad);
    //$('#dtTimeseries').on('xhr.dt', { 'chkbxId': '#chkbxSelectAllTS' }, dataTableLoad);

    //Add click handlers...

    //Avoid multiple registrations of the same handler...
    $('#chkbxSelectAllTS').off('click', selectAll);
    $('#chkbxSelectAllTS').on('click', { 'tableId': 'dtTimeseries', 'chkbxId': 'chkbxSelectAllTS', 'btnId': 'btnZipSelectionsTS', 'btnClearId': 'btnClearSelectionsTS'}, selectAll);

    //Avoid multiple registrations of the same handler...
    $('#btnZipSelectionsTS').off('click', zipSelections);
    $('#btnZipSelectionsTS').on('click', { 'tableId': 'dtTimeseries', 'chkbxId': 'chkbxSelectAllTS'}, zipSelections);

    //Avoid multiple registrations of the same handler...
    $('#btnClearSelectionsTS').off('click', clearSelections);
    $('#btnClearSelectionsTS').on('click', { 'tableId': 'dtTimeseries', 'chkbxId': 'chkbxSelectAllTS', 'btnId': 'btnZipSelectionsTS', 'btnClearId': 'btnClearSelectionsTS' }, clearSelections);

    //Add DataTables event handlers...
    //Search event...
    $('#dtTimeseries').off('search.dt', dtSearchOrOrder);
    $('#dtTimeseries').on('search.dt', { 'tableId': 'dtTimeseries', 'chkbxId': 'chkbxSelectAllTS' }, dtSearchOrOrder);

    //Order event...
    $('#dtTimeseries').off('order.dt', dtSearchOrOrder);
    $('#dtTimeseries').on('order.dt', { 'tableId': 'dtTimeseries', 'chkbxId': 'chkbxSelectAllTS' }, dtSearchOrOrder);
}

//DataTables search event handler...
function dtSearchOrOrder(event, settings) {
    //console.log( 'dtSearch called!!!')

    if ( $('#' + event.data.chkbxId).prop('checked')) {
        $('#' + event.data.chkbxId).triggerHandler('click');
    }
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
        //BCC - 26-Jun-2015 - QA Issue #9 - Timeout error message: typo in word 'occurred'
        bootbox.alert("<H4>An error occurred: '" + message + "'. Please limit search area or Keywords. Please contact Support if the problem persists.</H4>");

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

function GetAreaInSquareKilometers(path) {
    
    

    var result = parseFloat(google.maps.geometry.spherical.computeArea(path)) * 0.000001;
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

    //var xMin = Math.min(ne.lng(), sw.lng())
    //var xMax = Math.max(ne.lng(), sw.lng())
    //var yMin = Math.min(ne.lat(), sw.lat())
    //var yMax = Math.max(ne.lat(), sw.lat())

    var path = [];

    path.push(new google.maps.LatLng(ne.lat(), sw.lng())),
    path.push(ne),
    path.push(new google.maps.LatLng(sw.lat(), ne.lng())),
    path.push(sw)

    return path;
}

//Set up tooltips...
function setUpTooltips(elementId) {

    if ('dtMarkers' === elementId) {
        //dtMarkers table

        //Datatable number of records select...
        var jqueryObject = $('#dtMarkers_length');

        jqueryObject.tooltip('destroy');
        jqueryObject.tooltip({
            'animation': true,
            'placement': 'auto',
            'trigger': 'hover',
            'title': 'Select the number of table entries to view on one page...'
        });

        //'Top' 25 select...
        jqueryObject = $('#spanSelectAll');

        //Simulate btn-primary colors...
        var templateString = '<div class="tooltip" role="tooltip">' +
                                '<div class="tooltip-arrow"></div>' +
                                '<div class="tooltip-inner" style="color: #ffffff; background-color: #428bca; border-color: #357ebd;"></div>' + 
                             '</div>'

        jqueryObject.tooltip('destroy');
        jqueryObject.tooltip({
            'animation': true,
            'placement': 'auto',
            'trigger': 'hover',
            'title': 'Select the top 25 rows in the current order...',
            'template': templateString 
        });

        //'Process Selections' button...
        jqueryObject = $('#btnZipSelections');

        templateString = '<div class="tooltip" role="tooltip">' +
                                '<div class="tooltip-arrow"></div>' +
                                '<div style="color: green; background-color: ivory; border-color: red; font-size: 2em;">' +
                                   '<div>' +
                                    '<span  class="glyphicon glyphicon-hand-right"></span>' +
                                    '<span class="tooltip-inner" style="color: green; background-color: ivory;  border-color: red; margin-left: 0.5em; margin-right: 0.5em">' +
                                    '</span>' +
                                    '<span  class="glyphicon glyphicon-hand-left"></span>' +
                                   '</div>' +
                                '</div>' +
                             '</div>';

        var titleString = 'Your time series data is only a click away!!!';

        jqueryObject.tooltip('destroy');
        jqueryObject.tooltip({
            'animation': true,
            'placement': 'auto',
            'trigger': 'hover',
            'title': titleString,
            'template': templateString
        });

    }

}
