import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ChangelogDialog } from './changelog-dialog';

describe('ChangelogDialog', () => {
  let component: ChangelogDialog;
  let fixture: ComponentFixture<ChangelogDialog>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChangelogDialog]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ChangelogDialog);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
