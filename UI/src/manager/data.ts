// Mock data for the Vehicle Manager window (M2 prototype).
// This is static placeholder data so the window can be built and styled before
// the C# cascade model / bindings exist. Nothing here talks to the game yet.

// A single resolved attribute value plus where it came from in the cascade.
// `overridden` = this exact prefab/class has its own value; otherwise the value
// is inherited from `source` (a parent class or the global default).
export interface AttrState {
  value: number;
  overridden: boolean;
  source: string;
}

export interface PrefabNode {
  id: string;
  name: string;
  className: string;
  custom?: boolean;
  thumbnail?: string;
  probability: AttrState; // percent, 100 = vanilla
  maxSpeed: AttrState; // km/h
  acceleration: AttrState; // m/s^2
  braking: AttrState; // m/s^2
}

export interface ClassNode {
  name: string;
  editable?: boolean;
  custom?: boolean;
  // Class-level override state (only present from the backend, not in mock data).
  probability?: AttrState;
  maxSpeed?: AttrState;
  acceleration?: AttrState;
  braking?: AttrState;
  prefabs: PrefabNode[];
}

export interface CategoryNode {
  key: string;
  name: string;
  icon: string;
  classes: ClassNode[];
}

const inherited = (value: number, source = "global"): AttrState => ({
  value,
  overridden: false,
  source,
});

const override = (value: number): AttrState => ({
  value,
  overridden: true,
  source: "",
});

export const VEHICLE_TREE: CategoryNode[] = [
  {
    key: "cars",
    name: "Cars",
    icon: "coui://uil/Standard/Car.svg",
    classes: [
      {
        name: "Sedan",
        prefabs: [
          {
            id: "Car01",
            name: "City sedan 01",
            className: "Sedan",
            probability: override(75),
            maxSpeed: override(140),
            acceleration: inherited(5, "Sedan"),
            braking: inherited(8, "Sedan"),
          },
          {
            id: "Car02",
            name: "City sedan 02",
            className: "Sedan",
            probability: inherited(100, "Sedan"),
            maxSpeed: inherited(150, "Sedan"),
            acceleration: inherited(5, "Sedan"),
            braking: inherited(8, "Sedan"),
          },
        ],
      },
      {
        name: "SUV",
        prefabs: [
          {
            id: "SUV01",
            name: "Family SUV 01",
            className: "SUV",
            probability: inherited(100),
            maxSpeed: inherited(160),
            acceleration: inherited(4, "SUV"),
            braking: inherited(7, "SUV"),
          },
        ],
      },
      {
        name: "Unclassified",
        prefabs: [
          {
            id: "CustomCarA",
            name: "MyCustomCar_Roadster",
            className: "Unclassified",
            custom: true,
            probability: inherited(100),
            maxSpeed: inherited(180),
            acceleration: inherited(6),
            braking: inherited(9),
          },
          {
            id: "CustomCarB",
            name: "MyCustomCar_Limo",
            className: "Unclassified",
            custom: true,
            probability: inherited(100),
            maxSpeed: inherited(130),
            acceleration: inherited(4),
            braking: inherited(7),
          },
        ],
      },
    ],
  },
  {
    key: "trains",
    name: "Trains",
    icon: "coui://uil/Standard/Train.svg",
    classes: [
      {
        name: "Passenger train",
        prefabs: [
          {
            id: "Train01",
            name: "Regional train",
            className: "Passenger train",
            probability: inherited(100),
            maxSpeed: override(120),
            acceleration: override(2),
            braking: inherited(4, "Passenger train"),
          },
        ],
      },
    ],
  },
  {
    key: "service",
    name: "Service",
    icon: "coui://uil/Standard/Wrench.svg",
    classes: [
      {
        name: "Police",
        prefabs: [
          {
            id: "Police01",
            name: "Police car 01",
            className: "Police",
            probability: inherited(100),
            maxSpeed: inherited(170),
            acceleration: inherited(6),
            braking: inherited(9),
          },
        ],
      },
    ],
  },
];

export interface PackInfo {
  name: string;
  readOnly: boolean;
}

export const MOCK_PACKS: { yours: PackInfo[]; shared: PackInfo[] } = {
  yours: [
    { name: "My city", readOnly: false },
    { name: "Realistic trains (copy)", readOnly: false },
  ],
  shared: [
    { name: "Vanilla", readOnly: true },
    { name: "Fewer motorcycles", readOnly: true },
    { name: "Realistic speeds", readOnly: true },
  ],
};
