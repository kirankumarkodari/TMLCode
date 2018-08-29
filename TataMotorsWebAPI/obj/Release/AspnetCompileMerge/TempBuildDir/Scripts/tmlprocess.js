//Confirmation Message before close Page
/* window.addEventListener("beforeunload", function (e) {
     var confirmationMessage = "\Are you sure you want to close";

     (e || window.event).returnValue = confirmationMessage; //Gecko + IE
     return confirmationMessage;                            //Webkit, Safari, Chrome
 }); */
//Check File Upload or Not

var FileProcessingState = {  processInit : 0,  validateFiles : 1, convertingAllFiles : 2, dataAlreadyExisted : 3,
                             processConsumptionFile : 4, processGenerationFile : 5,
                             processTimeSlotConsumptionFile : 6, processMeterConsumptionFile : 7, processOpenAccessBillFile : 8,
                             processAdditionalParamsInfo : 9, Completed : 10, Failed : 11 };
var currState = FileProcessingState.processInit;
var CompltedWithOverwrite = 914, CompltedWithoutOverwrite = 915;
var chat;
var IsAnyFieldEmpty = 0;  //Flag to check data is entered in All fields or not. 1-empty, 0-all fields are filled

function chkFile() {
    IsAnyFieldEmpty = 0;
    resetFieldStyle();
    validateFields();
    if (($('#ConsumptionFile')[0].files.length == 0) &&
        ($('#GenerationFile')[0].files.length == 0) &&
        ($('#TimeSlotConsumptionFile')[0].files.length == 0) &&
        ($('#MeterConsumptionFile')[0].files.length == 0) &&
        ($('#OpenAccessFile')[0].files.length == 0)
        ) {
        //(document.getElementById('ConsumptionFile').files.length === 0) && (document.getElementById('GenerationFile').files.length === 0)) {
        // Boht File uploads do not have files
        alertModal("Please choose Consumption, Generation, Time Slot Consumption, Meter Consumption & Open Access files");
    }
    else if ($('#ConsumptionFile')[0].files.length == 0) {
        // File upload do not have Cosumption file
        alertModal("Please choose Consumption file");
    }
    else if ($('#GenerationFile')[0].files.length == 0) {
        // File upload do not have Generation file
        alertModal("Please choose Generation file");
    }
    else if ($('#TimeSlotConsumptionFile')[0].files.length == 0) {
        // File upload do not have TimeSlot Consumption file
        alertModal("Please choose Time Slot Consumption file");
    }
    else if ($('#MeterConsumptionFile')[0].files.length == 0) {
        // File upload do not have TimeSlot Consumption file
        alertModal("Please choose Meter Consumption file");
    }
    else if ($('#OpenAccessFile')[0].files.length == 0) {
        // File upload do not have TimeSlot Consumption file
        alertModal("Please choose Open Access file");
    }
    else if (IsAnyFieldEmpty == 1) {
        alertModal("Oops! It seems to be some of field values are not entered")
    }
    else {
        var Consfilename = $('#ConsumptionFile')[0].files[0].name;
        var ConsfileExt = Consfilename.split(/[. ]+/).pop().toLowerCase();

        var Genfilename = $('#GenerationFile')[0].files[0].name;
        var GenfileExt = Genfilename.split(/[. ]+/).pop().toLowerCase();
        
        var TimeSlotConsfilename = $('#TimeSlotConsumptionFile')[0].files[0].name;
        var TimeSlotConsfileExt = TimeSlotConsfilename.split(/[. ]+/).pop().toLowerCase();

        var MeterConsfilename = $('#MeterConsumptionFile')[0].files[0].name;;
        var MeterConsfileExt = MeterConsfilename.split(/[. ]+/).pop().toLowerCase();

        var OpenAccessfilename = $('#OpenAccessFile')[0].files[0].name;;
        var OpenAccessfileExt = OpenAccessfilename.split(/[. ]+/).pop().toLowerCase();



        if (ConsfileExt != "pdf" || GenfileExt != "pdf" || TimeSlotConsfileExt != "pdf" || MeterConsfileExt != "pdf" || OpenAccessfileExt != "pdf") {
            // Invalid PDF Files

            if (ConsfileExt != "pdf")
               document.getElementById("ConsumptionFile").value = "";

            if (GenfileExt != "pdf")
                document.getElementById("GenerationFile").value = "";

            if (TimeSlotConsfileExt != "pdf")
                document.getElementById("TimeSlotConsumptionFile").value = "";

            if (MeterConsfileExt != "pdf")
                document.getElementById("MeterConsumptionFile").value = "";

            if (OpenAccessfileExt != "pdf")
                document.getElementById("OpenAccessFile").value = "";

            alertModal("Please select PDF files only");
        }
        else {
            //Process has been starts                
            // Append form data  
            var form_data = new FormData();
            form_data.append("IsOverwrite", document.getElementById('chkOverwrite').checked);

           

            form_data.append("ConsumptionFile", $('#ConsumptionFile')[0].files[0]);
            form_data.append("GenerationFile", $('#GenerationFile')[0].files[0]);
            form_data.append("TimeSlotConsumptionFile", $("#TimeSlotConsumptionFile")[0].files[0]);
            form_data.append("MeterConsumptionFile", $("#MeterConsumptionFile")[0].files[0]);
            form_data.append("OpenAccessFile", $("#OpenAccessFile")[0].files[0]);
            
            // for MSEDCL & 3rd Party Values processing
            for (var fIdx = 1; fIdx < 25; fIdx++) {
                var fieldID = "Field" + fIdx;
                var RateValue = document.getElementById(fieldID).value;
                form_data.append(fieldID, RateValue);
            }

            // for 4 type Charges values processing
            form_data.append("CrossCharges", document.getElementById("CrossCharges").value);
            form_data.append("WheelingCharges", document.getElementById("WheelingCharges").value);
            form_data.append("TransCharges", document.getElementById("TransCharges").value);
            form_data.append("OthPurDiscnt", document.getElementById("OthPurDiscnt").value);


            $('#processing-modal').modal('show');
            uploadProcessStart();
            $.ajax({
                url: "Services/Process/uploadFiles",
                processData: false,  // tell jQuery not to process the data
                contentType: false,  // tell jQuery not to set contentType
                data: form_data,
                type: "POST",
                success: function (response) {
                    console.log("Server Response code is :" + response.Error_Code);
                    switch (response.Error_Code) {
                        case 0:
                        case 1:
                            $("#ProcessInit").hide();
                            $('#processing-modal').hide;
                            $("#FailureMsg").show();
                            $("#processlbl").text(response.Msg);
                            $("#btnOk").show();
                            break;                        

                        case 2: $('#processing-modal').modal('show');
                            console.log(response.Msg);
                            break;
                    }
                },
                error: function (response) {
                    $("#ProcessInit").hide();
                    $("#FailureMsg").show();
                    $("#processlbl").text("Oops! Unable to upload files to server");
                    $("#btnOk").show();
                    console.log(response);
                }
            })
        }

    }
}

function alertModal(altMsg)
{
    $("#fileAlertlbl").text(altMsg);
    $('#fileUploadAlert-modal').modal('show');
}

function uploadProcessStart() {
    // Reset the existed Modal Dialog
    $("#ProcessInit").show();
    $("#ProcessMsg").show();
    $("#InfoMsg").hide();
    $("#SucessMsg").hide();
    $("#FailureMsg").hide();
    $("#btnOk").hide();

    $('#ConsProgresbar').removeClass('active');
    $("#ConsProcess").hide();
    $('#ConsProgresbar').css('width', 0 + '%').attr('aria-valuenow', 0);
    $('#ConsProcPerc').text(0 + '%');

    $('#GenProgresbar').removeClass('active');
    $("#GenProcess").hide();
    $('#GenProgresbar').css('width', 0 + '%').attr('aria-valuenow', 0);
    $('#GenProcPerc').text(0 + '%');

    $('#TimeSlotConsProgresbar').removeClass('active');
    $("#TimeSlotConsProcess").hide();
    $('#TimeSlotConsProgresbar').css('width', 0 + '%').attr('aria-valuenow', 0);
    $('#TimeslotConsProcPerc').text(0 + '%');

    $('#MeterConsProgresbar').removeClass('active');
    $("#MeterConsProcess").hide();
    $('#MeterConsProgresbar').css('width', 0 + '%').attr('aria-valuenow', 0);
    $('#MeterConsProcPerc').text(0 + '%');

    $('#OpenAccessBillProgresbar').removeClass('active');
    $("#OpenAccessBillProcess").hide();
    $('#OpenAccessBillProgresbar').css('width', 0 + '%').attr('aria-valuenow', 0);
    $('#OpenAccessBillProcPerc').text(0 + '%');

    $("#processlbl").text("Processing...");


    //$("#dataOverwriteConfirm").hide();

    // Reference the auto-generated proxy for the hub.
    chat = $.connection.ProcessUpdateHub;
    // Create a function that the hub can call back to display messages.
    chat.client.updateProcessLog = function (message) {
        console.log(message);
    };

    chat.client.processStateChanged = function (state, msg, errorCode) {
        currState = state;
        switch (state) {
            case FileProcessingState.processInit:
            case FileProcessingState.dataAlreadyExisted:
            case FileProcessingState.validateFiles: 
            case FileProcessingState.convertingAllFiles:
                $("#ProcessInit").show();
                $("#processlbl").text(msg);
                break; 

            case FileProcessingState.processConsumptionFile:
            case FileProcessingState.processGenerationFile:
            case FileProcessingState.processTimeSlotConsumptionFile:
            case FileProcessingState.processAdditionalParamsInfo:
                $("#ProcessInit").hide();
                $("#ConsProcess").show();
                $("#GenProcess").show();
                $("#TimeSlotConsProcess").show();
                $("#MeterConsProcess").show();
                $("#OpenAccessBillProcess").show();
                $("#processlbl").text(msg);
                break;


            case FileProcessingState.Completed:
                $("#ProcessInit").hide();
                if (errorCode == CompltedWithoutOverwrite)  // Overwrite not checked by user & Data is already existed & send successfully completed..
                {
                    $("#InfoMsg").show();
                }
                else if (errorCode == CompltedWithOverwrite)  // Overwrite checked by user & (Overwrite) successfully completed
                {
                    $("#ConsProcess").hide();
                    $("#GenProcess").hide();
                    $("#TimeSlotConsProcess").hide();
                    $("#MeterConsProcess").hide();
                    $("#OpenAccessBillProcess").hide();
                    $("#SucessMsg").show();
                }
                $("#processlbl").text(msg);
                $("#btnOk").show();
                clearAllInputs();
                break;

            case FileProcessingState.Failed:
                $("#ProcessInit").hide();
                $("#FailureMsg").show();
                $("#processlbl").text(msg + ". Error Code is " + errorCode);
                $("#btnOk").show();
                clearAllInputs();

                //alert("Error :" + msg + "\n" + "Code: " + errorCode);
                break;
        }
        
    }

    chat.client.progessUpdate = function (percentage) {
        switch (currState) {
            case FileProcessingState.processConsumptionFile:
                $('#ConsProgresbar').css('width', percentage + '%').attr('aria-valuenow', 0);
                $('#ConsProcPerc').text(percentage + '%');
                if (percentage > 0 && percentage <= 99) {
                    $('#ConsProgresbar').addClass('active');
                }
                else if (percentage == 100) {
                    $('#ConsProgresbar').removeClass('active');
                }
                break;

            case FileProcessingState.processGenerationFile:
                $('#GenProgresbar').css('width', percentage + '%').attr('aria-valuenow', 0);
                $('#GenProcPerc').text(percentage + '%');
                if (percentage > 0 && percentage <= 99) {
                    $('#GenProgresbar').addClass('active');
                }
                else if (percentage == 100) {
                    $('#GenProgresbar').removeClass('active');
                }
                break;

            case FileProcessingState.processTimeSlotConsumptionFile:
                $('#TimeSlotConsProgresbar').css('width', percentage + '%').attr('aria-valuenow', 0);
                $('#TimeslotConsProcPerc').text(percentage + '%');
                if (percentage > 0 && percentage <= 99) {
                    $('#TimeSlotConsProgresbar').addClass('active');
                }
                else if (percentage == 100) {
                    $('#TimeSlotConsProgresbar').removeClass('active');
                }
                break;

            case FileProcessingState.processMeterConsumptionFile:
                $('#MeterConsProgresbar').css('width', percentage + '%').attr('aria-valuenow', 0);
                $('#MeterConsProcPerc').text(percentage + '%');
                if (percentage > 0 && percentage <= 99) {
                    $('#MeterConsProgresbar').addClass('active');
                }
                else if (percentage == 100) {
                    $('#MeterConsProgresbar').removeClass('active');
                }
                break;

            case FileProcessingState.processOpenAccessBillFile:
                $('#OpenAccessBillProgresbar').css('width', percentage + '%').attr('aria-valuenow', 0);
                $('#OpenAccessBillProcPerc').text(percentage + '%');
                if (percentage > 0 && percentage <= 99) {
                    $('#OpenAccessBillProgresbar').addClass('active');
                }
                else if (percentage == 100) {
                    $('#OpenAccessBillProgresbar').removeClass('active');
                }
                break;
        }
    }

    $.connection.hub.start();
}

function sendDataOverWriteResponse(opt) {
    chat.server.sendDataOverwrite(opt);
    // $("#dataOverwriteConfirm").hide();
    $("#ProcessInit").show();
}
function clearAllInputs() {
    //clear Input Fields after completion
    document.getElementById("ConsumptionFile").value = "";
    document.getElementById("GenerationFile").value = "";
    document.getElementById("TimeSlotConsumptionFile").value = "";
    document.getElementById("MeterConsumptionFile").value = "";
    document.getElementById("OpenAccessFile").value = "";
    document.getElementById("chkOverwrite").checked = false;
}
//rates textboxvalidation checking 
//Restrict user from entering Special Characters in UserName Textbox ==added by Eswar 21June2016====
var specialKeys = new Array();
specialKeys.push(8); //Backspace
specialKeys.push(9); //Tab
specialKeys.push(46); //Delete
specialKeys.push(36); //Home
specialKeys.push(35); //End
specialKeys.push(37); //Left
specialKeys.push(39); //Right
function IsAlphaNumeric(e) {
    var keyCode = e.which ? e.which : e.keyCode
    var ret = ((keyCode >= 48 && keyCode <= 57) || specialKeys.indexOf(keyCode) != -1);
    $(".error").css("display", ret ? "none" : "inline");
    return ret;
}

//Fields Data Validation 
function validateFields() {
    // for MSDECL & 3rd 
    for (var i = 1; i < 25; i++) {
        var fieldID = "Field" + i;
        validateField(fieldID);
    }
    validateField("CrossCharges");
    validateField("WheelingCharges");
    validateField("TransCharges");
    validateField("OthPurDiscnt");
}

function validateField(fieldID)
{
    if (document.getElementById(fieldID).value == "") {
        IsAnyFieldEmpty = 1;
        document.getElementById(fieldID).style.borderBottomColor = "red";
    }
}

function resetFieldStyle() {

    // for MSDECL & 3rd 
    for (var i = 1; i < 25; i++) {
        var fieldID = "Field" + i;
        document.getElementById(fieldID).style.borderBottomColor = "#70bbc8";
    }

    // for 4 types of charges
    document.getElementById("CrossCharges").style.borderBottomColor = "#70bbc8";
    document.getElementById("WheelingCharges").style.borderBottomColor = "#70bbc8";
    document.getElementById("TransCharges").style.borderBottomColor = "#70bbc8";
    document.getElementById("OthPurDiscnt").style.borderBottomColor = "#70bbc8";
}
