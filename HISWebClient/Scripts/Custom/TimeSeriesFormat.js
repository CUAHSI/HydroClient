//
//A JavaScript 'enum' to time series format values
//NOTE: Values defined here and in the C# equivalent MUST always match!!

//Implementation - Dynamic Prototype Pattern...

// Usage:   var timeSeriesFormat = (new TimeSeriesFormat()).getEnum();
//          timeSeriesFormat.WaterOneFlow - 'enum' value - 2
//          timeSeriesFormat.properties[timeSeriesFormat.WaterOneFlow].description - 'enum' description - 'WaterOneFlow...'

function TimeSeriesFormat() {

    //Enum values...needs to be in multiples of 2 becaus of the [FLAGS] attribute of Model 
    this.enumValues = {
        'CSV': [1, 'CSV...'],
        'WaterOneFlow': [2, 'WaterOneFlow...'],
        'CSVMerged': [4, 'CSV Merged...']
    };

    if ("function" !== typeof this.getEnum) {
        TimeSeriesFormat.prototype.getEnum = function () {
            return (freezeEnum(this.enumValues));
        }
    }
}

