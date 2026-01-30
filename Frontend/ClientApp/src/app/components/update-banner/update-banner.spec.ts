import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UpdateBanner } from './update-banner';

describe('UpdateBanner', () => {
  let component: UpdateBanner;
  let fixture: ComponentFixture<UpdateBanner>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UpdateBanner]
    })
    .compileComponents();

    fixture = TestBed.createComponent(UpdateBanner);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
