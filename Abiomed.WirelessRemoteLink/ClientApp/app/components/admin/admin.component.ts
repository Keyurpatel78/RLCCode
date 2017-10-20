import { Component, OnInit} from '@angular/core';
import { AuthenticationService } from "../../shared/authentication.service";
import { Router} from '@angular/router';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { UserRegistrationInterface } from "../../shared/authentication.interface";
import { AuthGuard } from "../../shared/authguard.service";

@Component({
    selector: 'admin',
    templateUrl: './admin.component.html',
    styleUrls: ['./admin.component.css'],    
    providers: [AuthenticationService]
})
export class AdminComponent implements OnInit {
    createForm: FormGroup;
    firstname: string;
    lastname: string;
    phone1: string;
    phone2: string;
    phone3: string;
    email: string;
    role: string;
    created: boolean;
    creationStatus: string;
    creationResponse: string;
    constructor(private authenticationService: AuthenticationService) { }

    ngOnInit() {
        this.ValidateForm();
        this.created = false;
        this.creationStatus = "";
        this.creationResponse = "";
    }

    private ValidateForm() {
        this.createForm = new FormGroup({
            firstname: new FormControl('', [Validators.required]),
            lastname: new FormControl('', [Validators.required]),
            email: new FormControl('', [Validators.required, Validators.email]),
            role: new FormControl('', [Validators.required]),
            phone1: new FormControl(''),
            phone2: new FormControl(''),
            phone3: new FormControl('')
        });
    }   

    _drop(event: any) {
        event.preventDefault();
    }

    _keyPress(event: any) {
        console.log(event);
        const pattern = /[0-9]/;
        let inputChar = String.fromCharCode(event.charCode);

        if (!pattern.test(inputChar)) {
            // invalid character, prevent input
            event.preventDefault();
        }
    }

    private Register() {
        var roles = Array<string>();
        roles.push(this.role);

        var userRegistration = {
            FirstName: this.firstname,
            LastName: this.lastname,
            Phone: this.phone1 + this.phone2 + this.phone3,
            Email: this.email,
            Roles: roles,
            AcceptedTermsAndConditions: false,
            Activated: true,
            EmailConfirmed: true,
            PhoneConfirmed: true
        }

        // Clear form prior to submitting
        this.createForm.reset();

        this.authenticationService.registerUser(userRegistration).subscribe(result => {

            // Set message and show div
            this.creationStatus = result.creationResult;            
            this.creationResponse = result.response;
            this.created = true;

            //Put message up for 5 seconds
            setTimeout(() => {    //<<<---    using ()=> syntax
                this.created = false;
            }, 5000);
        });                     
    }
}


