$(function () {
   
  
    /* Load tree from Ajax JSON
     */
    $("#tree").fancytree({
        debugLevel: 0,  //BC - 26-Jun-2015 - Turn off console logging...
        checkbox: true,
        selectMode: 3,
        extensions: ["childcounter", "filter"],
        quicksearch: true,
        autoCollapse:false,
        source: {
            //url: "ajax-tree-plain.json"
            url: "/home/getOntologyMainCategories"
        },
        filter: {
            mode: "hide",
            autoApply: true
        },
        //activate: function (event, data) {
        //    var title = data.node.title;
        //    //FT.debug("activate: event=", event, ", data=", data);
        //    if (!$.isEmptyObject(title)) {
        //        //alert("custom node data: " + JSON.stringify(title));
        //    }
        //},
        lazyLoad: function (event, data) {
             //we can't return values from an event handler, so we
             //pass the result as `data`attribute.
                         // data.result = {url: "unit/ajax-sub2.json"};
            data.result = $.ajax({
                url: "/home/getOntologyByCategory/" + data.node.title,
               
                dataType: "json"
            });
        },
        //click: keywordClickHandler,
        //activate: keywordActivateHandler,
        select: keywordSelectHandler,
        //BCC - 20-Jul-2015 - Add intialization event handler...
        init: function (event, data) {            
            //Load keywords into fancytree...
            loadKeywordsIntoTree(false, 'btnSelectKeywords', 'Select Keyword(s)', 'glyphiconSpanSK');
        }
        //BC - 19-Jun-2015 - Disable concept counting - possible later use...
        //BC - Add select handler...
        //select: function (event, data) {
        //    var tree = $("#tree").fancytree("getTree");

        //    //Check for selected 'second-level' nodes (direct children of the 'top-level' nodes)
        //    var selectedNodes = tree.getSelectedNodes(true);
        //    var length = selectedNodes.length;
        //    var toplevelCount = 0;

        //    for (var i = 0; i < length; ++i) {
        //        var parent = selectedNodes[i].parent;

        //        ((null !== parent) && (parent.isTopLevel())) ? ++toplevelCount : toplevelCount;
        //    }

        //    if (selectedConceptsMax <= toplevelCount) {
        //        //Maximum 'top-level' nodes selected - make all unselected 'top-level' nodes 'unselectable' ...
        //        tree.visit(function (node) {                    
        //            if (node.isTopLevel() && (!node.isSelected())) {
        //                node.unselectable = true;
        //                //node.hideCheckbox = true;
        //                node.render();
        //            }
        //        });
        //    }
        //    else {
        //        //Maximum 'top-level' nodes NOT selected - make all unselected 'top-level' nodes 'selectable'...
        //        tree.visit(function (node) {
        //            if (node.isTopLevel() && (!node.isSelected())) {
        //                node.unselectable = false;
        //                //node.hideCheckbox = false;
        //                node.render();
        //            }
        //        });
        //    }
        //}
    });
    

    var tree = $("#tree").fancytree("getTree");

    //$('a[data-toggle="tab"]').on('click', function (e) {
    //    $("#tree").fancytree("getTree").visit(function (node) {
    //        node.setSelected(false);
    //    });
    //    return false;
    //    // activated tab
    //})

    /*
     * Event handlers for our little demo interface
     */
    $("input[name=search]").keyup(function(e){
        var n,
          leavesOnly = $("#leavesOnly").is(":checked"),
          match = $(this).val();

        if(e && e.which === $.ui.keyCode.ESCAPE || $.trim(match) === ""){
            $("button#btnResetSearch").click();
            return;
        }
        if($("#regex").is(":checked")) {
            // Pass function to perform match
            n = tree.filterNodes(function(node) {
                return new RegExp(match, "i").test(node.title);
            }, leavesOnly);
        } else {
            // Pass a string to perform case insensitive matching
            n = tree.filterNodes(match, leavesOnly);
        }
        $("button#btnResetSearch").attr("disabled", false);
        $("span#matches").text("(" + n + " matches)");
    }).focus();

    $("button#btnResetSearch").click(function(e){
        $("input[name=search]").val("");
        $("span#matches").text("");
        tree.clearFilter();
    }).attr("disabled", true);

    $("input#hideMode").change(function(e){
        tree.options.filter.mode = $(this).is(":checked") ? "hide" : "dimm";
        tree.clearFilter();
        $("input[name=search]").keyup();
    }).prop("checked", true);
    $("input#leavesOnly").change(function(e){
        // tree.options.filter.leavesOnly = $(this).is(":checked");
        tree.clearFilter();
        $("input[name=search]").keyup();
    });
    $("input#regex").change(function(e){
        tree.clearFilter();
        $("input[name=search]").keyup();
    });

    //Turn off fancy tree console logging
    //Source: https://github.com/mar10/fancytree/issues/29

    $.ui.fancytree.debugLevel = 0; // 0:quiet, 1:info, 2:debug
});

    
