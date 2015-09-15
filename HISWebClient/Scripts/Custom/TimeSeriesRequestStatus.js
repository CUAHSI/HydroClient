//
//A JavaScript 'enum' to time series request status values
//NOTE: Values defined here and in the C# equivalent MUST always match!!

//Implementation - Dynamic Prototype Pattern...

// Usage:   var timeSeriesRequestStatus = (new TimeSeriesRequestStatus()).getEnum();
//          timeSeriesRequestStatus.Completed - 'enum' value - 5
//          timeSeriesRequestStatus.properties[timeSeriesRequestStatus.Completed].description - 'enum' description - 'Completed!!'

function TimeSeriesRequestStatus() {

    //Enum values...
    this.enumValues = { "Starting": [1, "Starting..."],
                        "Started": [2, "Started!!"],
                        "ClientSubmittedCancelRequest": [3, "Client submitted cancellation request..."],
                        "CanceledPerClientRequest": [4, "Canceled per client request!!"],
                        "Completed": [5, "Completed!!"],
                        "ProcessingTimeSeriesId": [6, "Processing Time Series ID: "],
                        "SavingZipArchive": [7, "Saving Zip archive..."],
                        "ProcessingError": [8, "Processing error: "],
                        "UnknownTask": [9, "Unknown Task!!"],
                        "RequestTimeSeriesError": [10,"Request Time Series Error"], //Not currently used
                        "CheckTaskError": [11,"Check Task Error"],
                        "EndTaskError": [12,"End Task Error"]
                    };

    if ("function" !== typeof this.getEnum) {
        TimeSeriesRequestStatus.prototype.getEnum = function () {
            return (freezeEnum(this.enumValues));
        }
    }
}