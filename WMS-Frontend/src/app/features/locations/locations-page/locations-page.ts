import { Component, signal } from '@angular/core';
import { LocationNode } from '../models/location';

@Component({
  selector: 'app-locations-page',
  templateUrl: './locations-page.html',
  styleUrl: './locations-page.css',
})
export class LocationsPage {
  readonly locationTree = signal<LocationNode[]>([]);
}
