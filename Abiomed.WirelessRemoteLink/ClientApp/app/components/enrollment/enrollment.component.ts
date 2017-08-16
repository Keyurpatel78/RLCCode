import { Component} from '@angular/core';
import { AuthenticationService } from "../service/authentication.service";

@Component({
    selector: 'enrollment',
    templateUrl: './enrollment.component.html',
    styleUrls: ['./enrollment.component.css'],
    providers: [AuthenticationService]
})

export class EnrollmentComponent{        
    
}
