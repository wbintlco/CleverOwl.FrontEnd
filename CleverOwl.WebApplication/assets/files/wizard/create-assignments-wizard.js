//== Class definition
var AssignmentWizard = function () {
    //== Base elements
    var wizardEl = $('#m_wizard');
    var btnNext = wizardEl.find('[data-wizard-action="next"]');
    var btnPrev = wizardEl.find('[data-wizard-action="prev"]');
    var btnComplete = wizardEl.find('[data-wizard-action="submit"]');
    var dataName = "PropertyKey";
    var formEl;
    var currentStep;
    var currentUrl;
    var validator;
    var wizard;
    var dataValue;
    var minutesFromLastSMS = 0;
    var nextClicked = false;
    var backClicked = false;
    function refreshCurrentStep() {
        //currentStep = wizardEl.find('.m-wizard__form-step--current');
        //formEl = $('#' + currentStep.data('data-form-id'));
        //currentUrl = currentStep.data('url');
        //validator = formEl.validate({
        //    //== Validate only visible fields
        //    ignore: ":hidden"
        //});
        //if (dataValue != null) {
        //    $("input[name='" + dataName + "']").val(dataValue);
        //}

        wizard.start();
        if (wizard.getStep() == 1) {
            $('#assignmentNameHelp').hide();
            $('#descriptionHelp').hide();
            $('#pageImageDropZoneHelp').hide();
            $('#assignmentGradeHelp').hide();
            $('#assignmentSubjectHelp').hide();
            $('#assignmentSchoolHelp').hide();
            $('#assignmentCourseHelp').hide();
            var assigName = $('#assignmentName').val();
            var grade = $('#GradesDropDown').val();
            var subject = $('#SubjectsDropDown').val();
            var school = $('#SchoolsDropDown').val();
            var messageData = btoa(unescape(encodeURIComponent($('#description').summernote('code') + " ")));
            var file = filesGeted;
            var missedField = false;
            if (assigName == '') {
                mApp.unprogress(btnNext);
                mApp.unblock(wizardEl);
                missedField = true;
                $('#assignmentNameHelp').show();
                $('#assignmentNameHelp').addClass('empty_field');
                wizard.stop();
            }
            if (grade == '') {
                mApp.unprogress(btnNext);
                mApp.unblock(wizardEl);
                missedField = true;
                $('#assignmentGradeHelp').show();
                $('#assignmentGradeHelp').addClass('empty_field');
                wizard.stop();
            }
            if (subject == '') {
                mApp.unprogress(btnNext);
                mApp.unblock(wizardEl);
                missedField = true;
                $('#assignmentSubjectHelp').show();
                $('#assignmentSubjectHelp').addClass('empty_field');
                wizard.stop();
            }
            if (school == '') {
                mApp.unprogress(btnNext);
                mApp.unblock(wizardEl);
                missedField = true;
                $('#assignmentSchoolHelp').show();
                $('#assignmentSchoolHelp').addClass('empty_field');
                wizard.stop();
            }
            var scormLesson = $("#quicksearch_input_AutoCorrected").val();
            if (scormLesson.length == 0) {
                if (messageData == '' || messageData == 'IA==') {
                    mApp.unprogress(btnNext);
                    mApp.unblock(wizardEl);
                    missedField = true;
                    $('#descriptionHelp').show();
                    $('#descriptionHelp').addClass('empty_field');
                    wizard.stop();
                }
            }
            if (missedField == true) {
                var position = $('.empty_field').first().offset();
                $('html, body').animate({
                    scrollTop: position.top - 150
                }, 1000);
                $('.empty_field').removeClass('empty_field');
            }

            if (isCreate == "True") {
                var scormLessonDropdown = $("#quicksearch_input_AutoCorrected").val();
                if (scormLessonDropdown.length != 0) {
                    $("#onlineTextCheckBox").prop("checked", true);
                    $("#fileCheckBox").prop("checked", false);
                    $(".fileSubmission").hide();
                    $(".onlineText").show();
                } else {
                    $("#onlineTextCheckBox").prop("checked", true);
                    $("#fileCheckBox").prop("checked", true);
                    $(".fileSubmission").show();
                    $(".onlineText").show();
                }
            }

        }
        if (wizard.getStep() == 2) {
            $('#submissionDateHelp').hide();
            $('#dueDateHelp').hide();
            $('#reminderDateHelp').hide();
            $('#dueDateLowerThanSubDate').hide();
            $('#reminderDateLowerThanSubDate').hide();
            $('#reminderDateLowerThanDueDate').hide();
            var submissionDate = $('#submissionDate').val();
            var dueDate = $('#dueDate').val();
            var reminderDate = $('#reminderDate').val();
            var missedField = false;

            if (submissionDate == '') {
                mApp.unprogress(btnNext);
                mApp.unblock(wizardEl);
                missedField = true;
                $('#submissionDateHelp').show();
                $('#submissionDateHelp').addClass('empty_field');
                wizard.stop();
            }
            if (dueDate == '') {
                mApp.unprogress(btnNext);
                mApp.unblock(wizardEl);
                missedField = true;
                $('#dueDateHelp').show();
                $('#dueDateHelp').addClass('empty_field');
                wizard.stop();
            }
            if (reminderDate == '') {
                mApp.unprogress(btnNext);
                mApp.unblock(wizardEl);
                missedField = true;
                $('#reminderDateHelp').show();
                $('#reminderDateHelp').addClass('empty_field');
                wizard.stop();
            }

        }
        if (wizard.getStep() == 3) {

        }
        if (wizard.getStep() == 4) {
            if (isCreate == 'True') {
                GetStudentsEnrolled();
            }

            if (isReassign == 'True') {
                GetStudentsEnrolled();
            }
        }
    }

    var initWizard = function () {
        //== Initialize form wizard
        wizard = new mWizard('m_wizard', {
            startStep: 1
        });

        //== Validation before going to next page
        wizard.on('beforeNext', function (wizardObj) {
            //mApp.block(wizardEl);
            mApp.progress(btnNext);
            refreshCurrentStep();
            //if (!validator.form()) {
            //    console.log("not validate");
            //}
            //mApp.unblock(wizardEl);
            //mApp.unprogress(btnNext);
            //mUtil.scrollTop();

        });

        //== Change event
        wizard.on('change', function (wizardObj) {
            mApp.unprogress(btnNext);
            mApp.unblock(wizardEl);
            var position = $('.head').offset().top - 80;
            $('html, body').animate({
                scrollTop: position
            }, 1000);
            //wizard.start();
        });
    }

    var initButtons = function () {

        btnNext.on('click', function (e) {

            if (wizard.getStep() == 2) {
                $('#bar_1').removeClass('bar-current');
                $('#bar_1').addClass('bar-done');
                $('#bar_2').removeClass('bar');
                $('#bar_2').addClass('bar-current');
            }
            if (wizard.getStep() == 3) {
                $('#bar_2').removeClass('bar-current');
                $('#bar_2').addClass('bar-done');
                $('#bar_3').removeClass('bar');
                $('#bar_3').addClass('bar-current');
            }
            if (wizard.getStep() == 4) {
                $('#bar_3').removeClass('bar-current');
                $('#bar_3').addClass('bar-done');
                $('#bar_4').removeClass('bar');
                $('#bar_4').addClass('bar-current');
            }
            if (wizard.getStep() == 5) {
                $('#bar_4').removeClass('bar-current');
                $('#bar_4').addClass('bar-done');
                $('#bar_5').removeClass('bar');
                $('#bar_5').addClass('bar-current');
            }

        });
        btnPrev.on('click', function (e) {
            if (wizard.getStep() == 4) {
                $('#bar_5').removeClass('bar-current');
                $('#bar_5').addClass('bar');
                $('#bar_4').removeClass('bar-done');
                $('#bar_4').addClass('bar-current');
            }
            if (wizard.getStep() == 3) {
                $('#bar_4').removeClass('bar-current');
                $('#bar_4').addClass('bar');
                $('#bar_3').removeClass('bar-done');
                $('#bar_3').addClass('bar-current');
            }
            if (wizard.getStep() == 2) {
                $('#bar_3').removeClass('bar-current');
                $('#bar_3').addClass('bar');
                $('#bar_2').removeClass('bar-done');
                $('#bar_2').addClass('bar-current');
            }
            if (wizard.getStep() == 1) {
                $('#bar_2').removeClass('bar-current');
                $('#bar_2').addClass('bar');
                $('#bar_1').removeClass('bar-done');
                $('#bar_1').addClass('bar-current');
            }

        });

        btnComplete.on('click', function (e) {


            refreshCurrentStep();


        });
    }

    return {
        // public functions
        init: function () {
            wizardEl = $('#m_wizard');
            formEl = $('#m_form');
            initWizard();
            initButtons();

        }
    };
}();

function GetStudentsEnrolled() {
    $('#submitId').prop('disabled', true);
    $('#enrolled_students').empty();
    $('#SchoolKey').val($('#SchoolsDropDown').val());
    $('#SubjectKey').val($('#SubjectsDropDown').val());
    $('#GradeKey').val($('#GradesDropDown').val());
    $('#GetStudentsEnrolledHidden').ajaxSubmit({
        beforeSubmit: function () {
            mApp.block($('#enrolled_students'));
        },
        success: function (data) {
            if (data == "404") {
                swal('There are no students enrolled in this class', '', 'warning');
                return;
            }
            console.log(selectedStudentsList);
            var html = '<label class="m-checkbox m-checkbox--air">';
            if (selectedStudentsList.length > 0 && data.length != selectedStudentsList.length) {
                html += 'All<input type="checkbox" class="all_students" data-child="students" />';
            } else {
                html += 'All<input type="checkbox" class="all_students" data-child="students" checked="checked" />';
            }
            html += '<span></span>';
            html += '</label>';
            html += '<ul>';
            if (selectedStudentsList.length > 0) {
                $.each(data, function (index, value) {
                    html += '<li>';
                    html += '<label class="m-checkbox">';
                    if (selectedStudentsList.includes(value.StudentKey)) {
                        html += '<input class="student" type="checkbox" name="student" value="' + value.StudentKey + '" checked="checked">' + value.StudentName;
                    } else {
                        html += '<input class="student" type="checkbox" name="student" value="' + value.StudentKey + '">' + value.StudentName;
                    }
                    html += '<span></span>';
                    html += '</label>';
                    html += '</li>';
                });
            } else {
                $.each(data, function (index, value) {
                    html += '<li>';
                    html += '<label class="m-checkbox">';
                    html += '<input class="student" type="checkbox" name="student" value="' + value.StudentKey + '" checked="checked">' + value.StudentName;
                    html += '<span></span>';
                    html += '</label>';
                    html += '</li>';
                });
            }
            html += '</ul>';
            $('#enrolled_students').append(html);
            var studentsEnrolled = data.length;
            nbStudentChecked = studentsEnrolled;
            $('.student').change(function () {
                if ($(this).is(":checked")) {
                    nbStudentChecked++;
                    if (nbStudentChecked == studentsEnrolled) {
                        $('.all_students').prop('checked', true);
                    }
                } else {
                    nbStudentChecked--;
                    $('.all_students').prop('checked', false);
                }
            });
            $('.all_students').change(function () {
                if ($(this).is(":checked")) {
                    $('.student').prop('checked', true);
                    nbStudentChecked = studentsEnrolled;
                } else {
                    $('.student').prop('checked', false);
                    nbStudentChecked = 0;
                }
            });
            mApp.unblock($('#enrolled_students'));
            $('#submitId').prop('disabled', false);
        },
        error: function (xhr, status, error) {
            mApp.unblock($('#enrolled_students'));
            swal("Oops!", "we couldn't get the students enrolled. Please try again later.", 'error');
            studentsEnrolled = 0;
        }
    });
}

jQuery(document).ready(function () {

    AssignmentWizard.init();
});
