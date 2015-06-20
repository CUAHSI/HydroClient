$(function () {
   
  
    /* Load tree from Ajax JSON
     */
    $("#tree").fancytree({
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
        activate: function (event, data) {
            var title = data.node.title;
            //FT.debug("activate: event=", event, ", data=", data);
            if (!$.isEmptyObject(title)) {
                //alert("custom node data: " + JSON.stringify(title));
            }
        },
        lazyLoad: function (event, data) {
             //we can't return values from an event handler, so we
             //pass the result as `data`attribute.
                         // data.result = {url: "unit/ajax-sub2.json"};
            data.result = $.ajax({
                url: "/home/getOntologyByCategory/" + data.node.title,
               
                dataType: "json"
            });
        }//,
        //BC - 19-Jun-2015 - Disable concept counting - possible later use...
        //BC - Add select handler...
        //select: function (event, data) {
        //    var tree = $("#tree").fancytree("getTree");

            //Check selected 'top' nodes
            //var selectedNodes = tree.getSelectedNodes(true);
            //var length = selectedNodes.length;

        //    if (selectedConceptsMax <= length) {
        //        //Maximum nodes 'top' selected - make all unselected nodes 'unselectable' ...
        //        tree.visit(function (node) {                    
        //            if (!node.isSelected()) {
        //                node.unselectable = true;
        //                node.hideCheckbox = true;
        //                node.render(true);
        //            }
        //        });
        //    }
        //    else {
        //        //Maximum nodes 'top' NOT selected - make all unselected nodes 'selectable'...
        //        tree.visit(function (node) {
        //            if (!node.isSelected()) {
        //                node.unselectable = false;
        //                node.hideCheckbox = false;
        //                node.render(true);
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


});

    
