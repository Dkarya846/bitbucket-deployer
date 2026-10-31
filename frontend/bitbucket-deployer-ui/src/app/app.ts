import { Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, MatButtonModule, MatIconModule, MatToolbarModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  protected readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    // Independent of authGuard's own check — covers direct navigation to /login and keeps the
    // toolbar's login state correct even when a guard isn't involved.
    this.authService.refresh().subscribe();
  }

  protected logout(): void {
    this.authService.logout().subscribe(() => this.router.navigateByUrl('/login'));
  }
}
