//from http://vislab-ccom.unh.edu/~briana/examples/gdropdown/
var visibleOverlay = []
var gisLayers = [];

var kml = [];





//define generic arcgis Layer


//******************
//classess to init control
function addLayerControl() {
    gisLayers.push({ name: 'usstates', layer: new google.maps.KmlLayer({ url: 'http://cuahsiarcgis.cloudapp.net/gisressource/us_states.kmz', preserveViewport: false }), visible: false });

    var TMSLayer = (function () {
        function t(map, baseUrlPattern, yFlip, name, zIndex, bounds) {
            var _map = map;
            var _zIndex = zIndex
            var layer = new google.maps.ImageMapType({
                getTileUrl: function (coord, zoom) {
                    var proj = _map.getProjection();
                    var zfactor = Math.pow(2, zoom);
                    // get Long Lat coordinates
                    var swCoord = proj.fromPointToLatLng(new google.maps.Point(coord.x * 256 / zfactor, (coord.y + 1) * 256 / zfactor));
                    var neCoord = proj.fromPointToLatLng(new google.maps.Point((coord.x + 1) * 256 / zfactor, coord.y * 256 / zfactor));
                    if (bounds) {
                        var tileLatLng = new google.maps.LatLngBounds(swCoord, neCoord);
                        if (!tileLatLng.intersects(bounds)) {
                            return "http://maps.gstatic.com/intl/en_us/mapfiles/transparent.png";
                        }
                    }
                    return baseUrlPattern.replace("$z", zoom).replace("$y", yFlip ? coord.y : (1 << zoom) - coord.y - 1).replace("$x", coord.x);
                },
                tileSize: new google.maps.Size(256, 256),
                isPng: true,
                name: name
            });
            this._map = _map;
            this._layer = layer;
            this._zIndex = _zIndex
        }
        t.prototype.setVisible = function (visible) {
            var overlayMaps = this._map.overlayMapTypes;
            // find the layer
            for (var i = 0, I = overlayMaps.length; i < I && overlayMaps.getAt(i) != this._layer; ++i);
            if (visible) {
                // add if the map was not already added
                if (i == overlayMaps.length) {
                    //overlayMaps.push(this._layer);
                    if (typeof this._zIndex === "undefined") {
                        overlayMaps.push(this._layer);
                    }
                    else {
                        overlayMaps.push(null); // create empty overlay entry
                        overlayMaps.setAt(this._zIndex, this._layer);
                    }


                }
            } else {
                // remove if the map was added
                if (i < overlayMaps.length) {
                    overlayMaps.removeAt(i);
                }
            }
        }
        t.prototype.remove = function () {
            this.setVisible(false);
        }
        t.prototype.setOpacity = function (opacity) {
            if (this._layer) {
                this._layer.setOpacity(opacity);
            }
        }
        t.prototype.getOpacity = function () {
            if (this._layer) {
                return this._layer.getOpacity();
            } else {
                return 1.0;
            }
        }
        t.prototype.setZIndex = function (zIndex) {
            var i = this.getZIndex();
            var overlayMaps = this._map.overlayMapTypes;
            overlayMaps.removeAt(i);
            overlayMaps.insertAt(zIndex, this._layer);
        }

        t.prototype.getZIndex = function () {
            var overlayMaps = this._map.overlayMapTypes;
            for (var i = 0, I = overlayMaps.length; i < I && overlayMaps.getAt(i) != this._layer; ++i);
            return i;
        }

        return t;
    })();

    //Define NHD WMS tiled layer
    var NHDLayer = new google.maps.ImageMapType({
        getTileUrl: function (coord, zoom) {
            var proj = map.getProjection();
            var zfactor = Math.pow(2, zoom);
            // get Long Lat coordinates
            var top = proj.fromPointToLatLng(new google.maps.Point(coord.x * 256 / zfactor, coord.y * 256 / zfactor));
            var bot = proj.fromPointToLatLng(new google.maps.Point((coord.x + 1) * 256 / zfactor, (coord.y + 1) * 256 / zfactor));

            //corrections for the slight shift in some servers not required here
            var deltaX = 0.0;
            var deltaY = 0.0;

            //create the Bounding box string
            var bbox = (top.lng() + deltaX) + "," +
                           (bot.lat() + deltaY) + "," +
                           (bot.lng() + deltaX) + "," +
                           (top.lat() + deltaY);

            //base WMS URL
            var url = "http://services.nationalmap.gov/arcgis/rest/services/nhd/MapServer/export?";
            url += "&f=image";
            url += "&bbox=" + bbox;
            url += "&size=256,256;"
            url += "&imageSR=102113";
            url += "&bboxSR=4326";
            url += "&format=png32";
            url += "&layerDefs=";
            url += "&layers=show:1,2,3,4,5,6";
            url += "&transparent=true";
            return url;                 // return URL for the tile

        },
        tileSize: new google.maps.Size(256, 256),
        isPng: true,
        name: "ESRI_NHD"
    });
    //add Land Cover WMS layer

    //https://raster.nationalmap.gov/arcgis/rest/services/LandCover/USGS_EROS_LandCover_NLCD/MapServer/export?
    //f=image&bbox=-78.75000000000636%2C34.30714385630241%2C-75.9375000000071%2C36.597889133083584&size=256%2C256&
    //imageSR=102113&bboxSR=4326&format=png32&layerDefs=&layers=show%3A5%2C6&transparent=true				
    var NLCDLayer = new google.maps.ImageMapType({
        getTileUrl: function (coord, zoom) {
            var proj = map.getProjection();
            var zfactor = Math.pow(2, zoom);
            // get Long Lat coordinates
            var top = proj.fromPointToLatLng(new google.maps.Point(coord.x * 256 / zfactor, coord.y * 256 / zfactor));
            var bot = proj.fromPointToLatLng(new google.maps.Point((coord.x + 1) * 256 / zfactor, (coord.y + 1) * 256 / zfactor));

            //corrections for the slight shift in some servers not required here

            var deltaX = 0.0;
            var deltaY = 0.0;

            //create the Bounding box string
            var bbox = (top.lng() + deltaX) + "," +
                           (bot.lat() + deltaY) + "," +
                           (bot.lng() + deltaX) + "," +
                           (top.lat() + deltaY);

            //base WMS URL
            var url = "https://raster.nationalmap.gov/arcgis/rest/services/LandCover/USGS_EROS_LandCover_NLCD/MapServer/export?";
            url += "&f=image";
            url += "&bbox=" + bbox;
            url += "&size=256,256";
            url += "&imageSR=102100";
            url += "&bboxSR=4326";
            url += "&format=png32";
            url += "&layerDefs=";
            url += "&layers=show:5,6";
            url += "&transparent=true";
            return url;                 // return URL for the tile

        },
        tileSize: new google.maps.Size(256, 256),
        isPng: true,
        name: "USGS_NLCD",
        opacity: 0.7
    });

    //start process to set up custom drop down
    //create the options that respond to click
    //var divOptions = {
    //    gmap: map,
    //    name: 'Option1',
    //    title: "This acts like a button or click event",
    //    id: "mapOpt",
    //    action: function () {
    //        alert('option1');
    //    }
    //}
    //var optionDiv1 = new optionDiv(divOptions);

    //var divOptions2 = {
    //    gmap: map,
    //    name: 'Option2',
    //    title: "This acts like a button or click event",
    //    id: "satelliteOpt",
    //    action: function () {
    //        alert('option2');
    //    }
    //}

    //var optionDiv2 = new optionDiv(divOptions2);

    //create the check box items
    var checkOptions = {
        gmap: map,
        title: "This allows for multiple selection/toggling on/off",
        id: "usstates",
        label: "US states",
        action: function (e) {
            for (var i = 0, len = gisLayers.length; i < len; i++) {
                if (gisLayers[i].name === 'usstates') {

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
        id: "ESRI_Hydrology",
        label: "ESRI Hydrology",
        action: function (e) {
             
            if (!map.overlayMapTypes.getAt(3)) {
                var tmsLayer = new TMSLayer(map, "http://hydrology.esri.com/arcgis/rest/services/WorldHydroReferenceOverlay/MapServer/tile/$z/$y/$x", true, "ESRI_HYDRO", 3);             
                tmsLayer.setOpacity(0.9);
                tmsLayer.setVisible(true); 
            }
            else {
                map.overlayMapTypes.removeAt(3)
            }
        }
    }
    var check2 = new checkBox(checkOptions2);

    var checkOptions3 = {
        gmap: map,
        title: "This allows for multiple selection/toggling on/off",
        id: "ESRI_HUC",
        label: "ESRI HUC",
        action: function () {
            if (!map.overlayMapTypes.getAt(2))
            {
                map.overlayMapTypes.push(null); // create empty overlay entry
                map.overlayMapTypes.setAt(2, NHDLayer);            
            }
            else 
            {
                map.overlayMapTypes.removeAt(2)
            }
            //map.overlayMapTypes.push(NHDLayer);
        }
    }
    var check3 = new checkBox(checkOptions3);

    var checkOptions4 = {
        gmap: map,
        title: "This allows for multiple selection/toggling on/off",
        id: "USGC LandCover 2011",
        label: "USGC LandCover 2011",
        action: function () {
            if (!map.overlayMapTypes.getAt(1)) {
                map.overlayMapTypes.push(null); // create empty overlay entry
                map.overlayMapTypes.setAt(1, NLCDLayer);
                //Add Legend
                var elem = document.createElement("img");
                elem.setAttribute("src", "/Content/Images/Legend/NLCD_Colour_Classification_Update.jpg");
                //elem.setAttribute("height", "768");
                //elem.setAttribute("width", "1024");
                elem.style.opacity = "0.7";
                elem.setAttribute("alt", "Legend");
                
                var myLegendDiv = document.createElement('div');
                myLegendDiv.appendChild(elem);
                myLegendDiv.id = "NLCD_Legend"
                map.controls[google.maps.ControlPosition.RIGHT_BOTTOM].push(myLegendDiv);
            }
            else {
                map.overlayMapTypes.removeAt(1)
                var indexOfControl = -1;
                rightCenterControls = map.controls[google.maps.ControlPosition.RIGHT_BOTTOM];
                rightCenterControls.forEach(function (element, index) {
                    if( element.id === 'NLCD_Legend' ) {
                        indexOfControl = index;
                    }
                } );
                if( indexOfControl>-1 ) {
                    rightCenterControls.removeAt(indexOfControl);
                }
            }           
        }
    }
    var check4 = new checkBox(checkOptions4);
    //create the input box items

    //possibly add a separator between controls        
    var sep = new separator();

    //put them all together to create the drop down       
    var ddDivOptions = {
        //items: [optionDiv1,3optionDiv2, sep, check1, check2],
        items: [check1, check2, check3, check4],
        id: "myddOptsDiv"
    }
    //alert(ddDivOptions.items[1]);
    var dropDownDiv = new dropDownOptionsDiv(ddDivOptions);

    var dropDownOptions = {
        gmap: map,
        name: 'Layer Control',
        id: 'ddControl',
        title: 'Add and remove map Layers',
        position: google.maps.ControlPosition.RIGHT_TOP,
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