import { Component } from '@angular/core';

@Component({
  selector: 'app-no-access',
  template: `
    <section
      class="rounded-md border border-[#d7e1ef] bg-white px-6 py-10 shadow-[0_4px_20px_rgba(24,48,83,0.03)]"
    >
      <h2 class="text-2xl font-bold text-[#14243b]">No workspace access</h2>
      <p class="mt-2 text-sm text-[#647590]">
        Your account does not have a workspace role. Contact your Tenant Administrator for access.
      </p>
    </section>
  `,
})
export class NoAccess {}
