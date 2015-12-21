//
//A JavaScript 'enum' to time series format values
//NOTE: Values defined here and in the C# equivalent MUST always match!!

//Implementation - Dynamic Prototype Pattern...

// Usage:   var timeSeriesFormat = (new TimeSeriesFormat()).getEnum();
//          timeSeriesFormat.WaterOneFlow - 'enum' value - 2
//          timeSeriesFormat.properties[timeSeriesFormat.WaterOneFlow].description - 'enum' description - 'WaterOneFlow...'

function TimeSeriesFormat() {

    //Enum values...
    this.enumValues = { 'CSV': [1, 'CSV...'],
                        'WaterOneFlow': [2, 'WaterOneFlow...']
                        };

    if ("function" !== typeof this.getEnum) {
        TimeSeriesFormat.prototype.getEnum = function () {
            return (freezeEnum(this.enumValues));
        }
    }
}

