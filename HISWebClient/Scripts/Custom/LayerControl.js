//from http://vislab-ccom.unh.edu/~briana/examples/gdropdown/

var gisLayers = [];
gisLayers.push({ name: 'usgssub', layer: new google.maps.KmlLayer({ url: 'http://cuahsiarcgis.cloudapp.net/gisressource/us_states.kmz', preserveViewport: false }), visible: false });

var kml = [];
//******************
//classess to init control
function addLayerControl() {
    //start process to set up custom drop down
    //create the options that respond to click
    var divOptions = {
        gmap: map,
        name: 'Option1',
        title: "This acts like a button or click event",
        id: "mapOpt",
        action: function () {
            alert('option1');
        }
    }
    var optionDiv1 = new optionDiv(divOptions);

    var divOptions2 = {
        gmap: map,
        name: 'Option2',
        title: "This acts like a button or click event",
        id: "satelliteOpt",
        action: function () {
            alert('option2');
        }
    }

    var optionDiv2 = new optionDiv(divOptions2);

    //create the check box items
    var checkOptions = {
        gmap: map,
        title: "This allows for multiple selection/toggling on/off",
        id: "usgssub",
        label: "On/Off",
        action: function (e) {
            for (var i = 0, len = gisLayers.length; i < len; i++) {
                if (gisLayers[i].name === 'usgssub') {

                    if (!gisLayers[i].visible) {
                        //var layer = new google.maps.KmlLayer({ url: 'http://cuahsiarcgis.cloudapp.net/gisressource/us_states.kmz', preserveViewport: false, map: map })
                        //layer.setMap(map);
                        //kml[gisLayers.name].obj = layer;

                        gisLayers[i].layer.setMap(map);
                        gisLayers[i].visible = true
                    }
                    else {
                        //kml[gisLayers.name].obj.setMap(null);
                        gisLayers[i].layer.setMap(null);
                        gisLayers[i].visible = false;
                    }
                }
            }




        }
    }
    var check1 = new checkBox(checkOptions);

    var checkOptions2 = {
        gmap: map,
        title: "This allows for multiple selection/toggling on/off",
        id: "myCheck",
        label: "my On/Off",
        action: function () {
            alert('you clicked check 2');
        }
    }
    var check2 = new checkBox(checkOptions2);

    //create the input box items

    //possibly add a separator between controls        
    var sep = new separator();

    //put them all together to create the drop down       
    var ddDivOptions = {
        items: [optionDiv1, optionDiv2, sep, check1, check2],
        id: "myddOptsDiv"
    }
    //alert(ddDivOptions.items[1]);
    var dropDownDiv = new dropDownOptionsDiv(ddDivOptions);

    var dropDownOptions = {
        gmap: map,
        name: 'My Box',
        id: 'ddControl',
        title: 'A custom drop down select with mixed elements',
        position: google.maps.ControlPosition.RIGHT_CENTER,
        dropDown: dropDownDiv
    }

    var dropDown1 = new dropDownControl(dropDownOptions);


}


/************
 Classes to set up the drop-down control
 ************/

function optionDiv(options) {
    var control = document.createElement('DIV');
    control.className = "dropDownItemDiv";
    control.title = options.title;
    control.id = options.id;
    control.innerHTML = options.name;
    google.maps.event.addDomListener(control, 'click', options.action);
    return control;
}

function checkBox(options) {
    //first make the outer container
    var container = document.createElement('DIV');
    container.className = "checkboxContainer";
    container.title = options.title;

    var span = document.createElement('SPAN');
    span.role = "checkbox";
    span.className = "checkboxSpan";

    var bDiv = document.createElement('DIV');
    bDiv.className = "blankDiv";
    bDiv.id = options.id;

    var image = document.createElement('IMG');
    image.className = "blankImg";
    image.src = "http://maps.gstatic.com/mapfiles/mv/imgs8.png";

    var label = document.createElement('LABEL');
    label.className = "checkboxLabel";
    label.innerHTML = options.label;

    bDiv.appendChild(image);
    span.appendChild(bDiv);
    container.appendChild(span);
    container.appendChild(label);

    google.maps.event.addDomListener(container, 'click', function (e) {
        (document.getElementById(bDiv.id).style.display == 'block') ? document.getElementById(bDiv.id).style.display = 'none' : document.getElementById(bDiv.id).style.display = 'block';
        options.action(e);
    })
    return container;
}
function separator() {
    var sep = document.createElement('DIV');
    sep.className = "separatorDiv";
    return sep;
}

function dropDownOptionsDiv(options) {
    //alert(options.items[1]);
    var container = document.createElement('DIV');
    container.className = "dropDownOptionsDiv";
    container.id = options.id;


    for (i = 0; i < options.items.length; i++) {
        //alert(options.items[i]);
        container.appendChild(options.items[i]);
    }

    //for(item in options.items){
    //container.appendChild(item);
    //alert(item);
    //}        
    return container;
}

function dropDownControl(options) {
    var container = document.createElement('DIV');
    container.className = 'controlContainer';

    var control = document.createElement('DIV');
    control.className = 'dropDownControl';
    control.innerHTML = options.name;
    control.id = options.name;
    var arrow = document.createElement('IMG');
    arrow.src = "http://maps.gstatic.com/mapfiles/arrow-down.png";
    arrow.className = 'dropDownArrow';
    control.appendChild(arrow);
    container.appendChild(control);
    container.appendChild(options.dropDown);

    options.gmap.controls[options.position].push(container);
    google.maps.event.addDomListener(container, 'click', function () {
        (document.getElementById('myddOptsDiv').style.display == 'block') ? document.getElementById('myddOptsDiv').style.display = 'none' : document.getElementById('myddOptsDiv').style.display = 'block';
        setTimeout(function () {
            //document.getElementById('myddOptsDiv').style.display = 'none';
        }, 5000);
    })
}

function buttonControl(options) {
    var control = document.createElement('DIV');
    control.innerHTML = options.name;
    control.className = 'button';
    control.index = 1;

    // Add the control to the map
    options.gmap.controls[options.position].push(control);

    // When the button is clicked pan to sydney
    google.maps.event.addDomListener(control, 'click', options.action);
    return control;
}