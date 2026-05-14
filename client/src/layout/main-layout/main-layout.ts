import { Component } from '@angular/core';
import { Navbar } from '../navbar/navbar';
import { SidebarComponent } from '../sidebar/sidebar';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-main-layout',
  imports: [Navbar, SidebarComponent, RouterModule],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss',
})
export class MainLayout {}
