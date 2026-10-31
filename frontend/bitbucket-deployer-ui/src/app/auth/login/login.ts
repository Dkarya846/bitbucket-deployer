import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [MatButtonModule, MatCardModule, MatIconModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  protected readonly hasError = this.route.snapshot.queryParamMap.has('error');

  protected login(): void {
    this.authService.login();
  }
}
