import { UmbUnpublishDocumentEntityAction } from '../entity-action/index.js';
import type { UmbDocumentVariantOptionModel } from '../../../types.js';
import { UMB_DOCUMENT_ENTITY_TYPE, UMB_DOCUMENT_UNPUBLISH_MODAL } from '../../../constants.js';
import { UmbDocumentPublishingRepository } from '../../repository/index.js';
import { umbConfirmModal, umbOpenModal } from '@umbraco-cms/backoffice/modal';
import { UmbEntityBulkActionBase } from '@umbraco-cms/backoffice/entity-bulk-action';
import { UMB_APP_LANGUAGE_CONTEXT, UmbLanguageCollectionRepository } from '@umbraco-cms/backoffice/language';
import { UmbVariantId } from '@umbraco-cms/backoffice/variant';
import { UmbLocalizationController } from '@umbraco-cms/backoffice/localization-api';
import { UMB_ENTITY_CONTEXT } from '@umbraco-cms/backoffice/entity';
import { UMB_ACTION_EVENT_CONTEXT } from '@umbraco-cms/backoffice/action';
import { UmbRequestReloadChildrenOfEntityEvent } from '@umbraco-cms/backoffice/entity-action';
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification';

export class UmbDocumentUnpublishEntityBulkAction extends UmbEntityBulkActionBase<object> {
	async execute() {
		const entityContext = await this.getContext(UMB_ENTITY_CONTEXT);
		if (!entityContext) {
			throw new Error('Entity context not found');
		}
		const entityType = entityContext.getEntityType();
		const unique = entityContext.getUnique();

		const notificationContext = await this.getContext(UMB_NOTIFICATION_CONTEXT);
		const localize = new UmbLocalizationController(this);

		if (!entityType) throw new Error('Entity type not found');
		if (unique === undefined) throw new Error('Entity unique not found');

		// If there is only one selection, we can refer to the regular unpublish entity action:
		if (this.selection.length === 1) {
			const action = new UmbUnpublishDocumentEntityAction(this._host, {
				unique: this.selection[0],
				entityType: UMB_DOCUMENT_ENTITY_TYPE,
				meta: {} as never,
			});
			await action.execute();
			return;
		}

		const languageRepository = new UmbLanguageCollectionRepository(this._host);
		const { data: languageData } = await languageRepository.requestCollection({});

		const options: UmbDocumentVariantOptionModel[] = (languageData?.items ?? []).map((language) => ({
			language,
			variant: {
				name: language.name,
				culture: language.unique,
				state: null,
				createDate: null,
				publishDate: null,
				updateDate: null,
				segment: null,
				scheduledPublishDate: null,
				scheduledUnpublishDate: null,
			},
			unique: new UmbVariantId(language.unique, null).toString(),
			culture: language.unique,
			segment: null,
		}));

		const eventContext = await this.getContext(UMB_ACTION_EVENT_CONTEXT);
		if (!eventContext) {
			throw new Error('Event context not found');
		}
		const event = new UmbRequestReloadChildrenOfEntityEvent({
			entityType,
			unique,
		});

		// If there is only one language available, we can skip the modal and unpublish directly:
		if (options.length === 1) {
			const localizationController = new UmbLocalizationController(this._host);
			const confirm = await umbConfirmModal(this, {
				headline: localizationController.term('actions_unpublish'),
				content: localizationController.term('prompt_confirmListViewUnpublish'),
				color: 'warning',
				confirmLabel: localizationController.term('actions_unpublish'),
			}).catch(() => false);

			if (confirm !== false) {
				const variantId = new UmbVariantId(options[0].language.unique, null);
				const publishingRepository = new UmbDocumentPublishingRepository(this._host);
				let documentCnt = 0;

				for (let i = 0; i < this.selection.length; i++) {
					const id = this.selection[i];
					const { error } = await publishingRepository.unpublish(id, [variantId]);

					if (!error) {
						documentCnt++;
					}
				}

				notificationContext?.peek('positive', {
					data: {
						headline: localize.term('speechBubbles_contentUnpublished'),
						message: localize.term('speechBubbles_editMultiContentUnpublishedText', documentCnt),
					},
				});

				eventContext.dispatchEvent(event);
			}
			return;
		}

		// Figure out the default selections
		// TODO: Missing features to pre-select the variant that fits with the variant-id of the tree/collection? (Again only relevant if the action is executed from a Tree or Collection) [NL]
		const selection: Array<string> = [];
		const context = await this.getContext(UMB_APP_LANGUAGE_CONTEXT);
		if (!context) throw new Error('App language context not found');
		const appCulture = context.getAppCulture();
		// If the app language is one of the options, select it by default:
		if (appCulture && options.some((o) => o.unique === appCulture)) {
			selection.push(new UmbVariantId(appCulture, null).toString());
		}

		const result = await umbOpenModal(this, UMB_DOCUMENT_UNPUBLISH_MODAL, {
			data: {
				options,
			},
			value: { selection },
		}).catch(() => undefined);

		if (!result?.selection.length) return;

		const variantIds = result?.selection.map((x) => UmbVariantId.FromString(x)) ?? [];

		const repository = new UmbDocumentPublishingRepository(this._host);

		if (variantIds.length) {
			let documentCnt = 0;
			for (const unique of this.selection) {
				const { error } = await repository.unpublish(unique, variantIds);

				if (!error) {
					documentCnt++;
				}
			}

			notificationContext?.peek('positive', {
				data: {
					headline: localize.term('speechBubbles_contentUnpublished'),
					message: localize.term(
						'speechBubbles_editMultiVariantUnpublishedText',
						documentCnt,
						localize.list(variantIds.map((v) => v.culture ?? '')),
					),
				},
			});

			eventContext.dispatchEvent(event);
		}
	}
}

export { UmbDocumentUnpublishEntityBulkAction as api };
