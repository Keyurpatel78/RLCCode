﻿import {Injectable} from '@angular/core';
import { HttpEvent, HttpInterceptor, HttpHandler, HttpRequest } from '@angular/common/http';
import { Observable } from "rxjs/Observable";

@Injectable()
export class GeneralInterceptor implements HttpInterceptor {
    
    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {

        var request = req.clone({ headers: req.headers.set('Content-Type', 'application/json') });

        return next.handle(request);
    }
}