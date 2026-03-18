import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CopyText } from './copy-text';

describe('CopyText', () => {
  let component: CopyText;
  let fixture: ComponentFixture<CopyText>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CopyText]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CopyText);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
