import { Injectable, Inject } from '@angular/core';

@Injectable()
export class ConnectionService {
  constructor(private http: HttpClient, @Inject('BASE_URL') baseUrl: string) {}

  test() {
    this.http.get(bas).map((res: Response) => {
      res.json();
    });
  }
}
